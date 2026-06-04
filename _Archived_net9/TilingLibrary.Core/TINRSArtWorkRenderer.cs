using ArtWork;
using GerberLibrary.Core;
using GlmNet;
using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TilingLibrary;
using TilingLibrary.Compatibility;
using Bitmap = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using Graphics = TilingLibrary.Compatibility.GraphicsInterface;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using SizeF = SixLabors.ImageSharp.SizeF;
using Font = SixLabors.Fonts.Font;
using FontStyle = SixLabors.Fonts.FontStyle;
using Pen = TilingLibrary.Compatibility.Primitives.Pen;
using SolidBrush = TilingLibrary.Compatibility.Primitives.SolidBrush;
using Matrix = TilingLibrary.Compatibility.Matrix;
using GraphicsPath = TilingLibrary.Compatibility.Primitives.GraphicsPath;
using DirectBitmap = TilingLibrary.Compatibility.DirectBitmap;
using ImageSharpGraphicsInterface = TilingLibrary.Compatibility.ImageSharpGraphicsInterface;
using FontFamily = TilingLibrary.Compatibility.Primitives.FontFamily;
using StringFormat = TilingLibrary.Compatibility.Primitives.StringFormat;
using Primitives = TilingLibrary.Compatibility.Primitives;

namespace Artwork
{
    using Path = List<ClipperLib.IntPoint>;
    using Paths = List<List<ClipperLib.IntPoint>>;

    public class SolidQuadTreeItem : QuadTreeItem
    {
        int _x;
        int _y;
        public int x
        {
            get
            {
                return _x;
            }

            set
            {
                _x = value;
            }
        }

        public int y
        {
            get
            {
                return _y;
            }

            set
            {
                _y = value;
            }
        }
    }


    public class TINRSArtWorkRenderer
    {
        public static Color InterpolateColor(Color color1, Color color2, float v)
        {
            var p1 = color1.ToPixel<Rgba32>();
            var p2 = color2.ToPixel<Rgba32>();
            
            float iv = 1.0f - v;
            var R = (byte)(p1.R * iv) + (byte)(p2.R * v);
            var G = (byte)(p1.G * iv) + (byte)(p2.G * v);
            var B = (byte)(p1.B * iv) + (byte)(p2.B * v);
            var A = (byte)(p1.A * iv) + (byte)(p2.A * v);
            return Color.FromRgba((byte)R, (byte)G, (byte)B, (byte)A);
        }

        // From: http://stackoverflow.com/a/11448060/368354
        public static void SaveAsIcon(List<Bitmap> SourceBitmaps, string FilePath)
        {
            FileStream FS = new FileStream(FilePath, FileMode.Create);
            // ICO header
            FS.WriteByte(0); FS.WriteByte(0); // reserved
            FS.WriteByte(1); FS.WriteByte(0); // type = icon
            FS.WriteByte((byte)SourceBitmaps.Count); FS.WriteByte(0); // number of images
            List<MemoryStream> Files = new List<MemoryStream>();
            List<long> SizeIdx = new List<long>();
            List<long> OffIdx = new List<long>();
            foreach (var b in SourceBitmaps)
            {
                // Image size
                // Set to 0 for 256 px width/height
                if (b.Width < 256) FS.WriteByte((byte)b.Width); else FS.WriteByte(0);
                if (b.Height < 256) FS.WriteByte((byte)b.Height); else FS.WriteByte(0);
                // Palette
                FS.WriteByte(0);
                // Reserved
                FS.WriteByte(0);
                // Number of color planes
                FS.WriteByte(1); FS.WriteByte(0);
                // Bits per pixel
                FS.WriteByte(32); FS.WriteByte(0);

                // Data size, will be written after the data
                SizeIdx.Add(FS.Length);
                FS.WriteByte(0);
                FS.WriteByte(0);
                FS.WriteByte(0);
                FS.WriteByte(0);

                OffIdx.Add(FS.Length);
                // Offset to image data
                FS.WriteByte(0);
                FS.WriteByte(0);
                FS.WriteByte(0);
                FS.WriteByte(0);

                MemoryStream MS = new MemoryStream();
                b.SaveAsPng(MS);
                MS.Seek(0, SeekOrigin.Begin);
                Files.Add(MS);
            }

            long CurrentOff = FS.Length;
            for (int i = 0; i < Files.Count; i++)
            {
                var F = Files[i];
                var OffTgt = OffIdx[i];
                var LenTgt = SizeIdx[i];
                long Len = F.Length;
                FS.Seek(LenTgt, SeekOrigin.Begin);
                FS.WriteByte((byte)Len);
                FS.WriteByte((byte)(Len >> 8));
                FS.WriteByte((byte)(Len >> 16));
                FS.WriteByte((byte)(Len >> 24));

                FS.Seek(OffTgt, SeekOrigin.Begin);
                FS.WriteByte((byte)CurrentOff);
                FS.WriteByte((byte)(CurrentOff >> 8));
                FS.WriteByte((byte)(CurrentOff >> 16));
                FS.WriteByte((byte)(CurrentOff >> 24));

                FS.Seek(0, SeekOrigin.End);

                F.CopyTo(FS);

                CurrentOff = FS.Length;


            }

            FS.Close();
        }


        public DelaunayBuilder Delaunay = new DelaunayBuilder();
        public QuadTreeNode MaskTree;
        public QuadTreeNode ArtTree;
        public Tiling.TilingDefinition TD = new Tiling.TilingDefinition();
        
        public List<Tiling.Polygon> SubDivPoly = new List<Tiling.Polygon>();

        public static Font GetAdjustedFont(Graphics GraphicRef, string GraphicString, Font OriginalFont, float ContainerWidth, float MaxFontSize, float MinFontSize, bool SmallestOnFail)
        {
            // We utilize MeasureString which we get via a control instance           
            for (float AdjustedSize = MaxFontSize; AdjustedSize >= MinFontSize; AdjustedSize--)
            {
                Font TestFont = SystemFonts.CreateFont(OriginalFont.Name, AdjustedSize);

                // Test the string with the new size
                var measuredSize = GraphicRef.MeasureString(GraphicString);
                SizeF AdjustedSizeNew = new SizeF((float)measuredSize.X, (float)measuredSize.Y);

                if (ContainerWidth > Convert.ToInt32(AdjustedSizeNew.Width))
                {
                    // Good font, return it
                    return TestFont;
                }
            }

            // If you get here there was no fontsize that worked
            // return MinimumSize or Original?
            if (SmallestOnFail)
            {
                return SystemFonts.CreateFont(OriginalFont.Name, MinFontSize);
            }
            else
            {
                return OriginalFont;
            }
        }


        public static void DrawIcon(int w, int h, Graphics G, string Label, float huerange = -1)
        {
            Bitmap Output = new Bitmap(w, h);
            Graphics g = new ImageSharpGraphicsInterface(Output);
            TINRSArtWorkRenderer Rend = new TINRSArtWorkRenderer();
            Settings TheSettings = Rend.GetHashSettings(Label, huerange);



            TheSettings.InvertSource = true;
            //TheSettings.MaxSubDiv = 4;

            Bitmap TileGfx = new Bitmap(w, h);
            Graphics G3 = new ImageSharpGraphicsInterface(TileGfx);
            G3.Clear(Color.Transparent);
            float D = 2;
            float x1 = D / 2;
            float x2 = w - x1;
            float y1 = D / 2;
            float y2 = h - x1;


            Font F = SystemFonts.CreateFont("Panton ExtraBold", h * 0.5f, FontStyle.Bold);
            F = GetAdjustedFont(g, Label, F, w * 0.9f, h * 0.9f, 3, false);
            var S = g.MeasureString(Label);
            var S2 = g.MeasureString("WO");
            S.Y = S2.Y;

            float BaseScale = w / 128.0f;

            GraphicsPath GP = new GraphicsPath();
            var fontFamily = new FontFamily() { Name = "Panton ExtraBold" };
            GP.AddString(Label, fontFamily, 0, F.Size * 1.333333f, new PointF(w / 2 - (float)S.X / 2, h / 2 - (float)S.Y / 2), new StringFormat());

            Bitmap M = new Bitmap(w, h);
            Graphics G2 = new ImageSharpGraphicsInterface(M);

            G2.Clear(Color.White);
            G2.CompositingQuality = Primitives.CompositingQuality.HighQuality;
            G2.InterpolationMode = Primitives.GraphicsInterpolationMode.High;
            G2.TextRenderingHint = Primitives.TextRenderingHint.AntiAlias;
            G2.SmoothingMode = Primitives.SmoothingMode.AntiAlias;

            RenderIconBackdrop(G2, Color.Black, TheSettings, x1, x2, y1, y2, 2, Rend);
            G2.DrawPath(new Pen(Color.Black, 5), GP.ToImageSharpPath());


            //            G2.DrawString(Letter.Text, F, new SolidBrush(Color.White), w / 2 - S.X / 2, h / 2 - S.Y / 2);
            G2.DrawPath(new Pen(Color.White, Math.Max(2.0f, 6 * BaseScale)), GP.ToImageSharpPath());
            G2.FillPath(new SolidBrush(Color.White), GP.ToImageSharpPath());

            g.DrawPath(new Pen(TheSettings.BackGroundColor, 5), GP.ToImageSharpPath());

            Rend.BuildTree(M, TheSettings);
            Rend.BuildStuff(M, TheSettings);

            g.CompositingQuality = Primitives.CompositingQuality.HighQuality;
            g.InterpolationMode = Primitives.GraphicsInterpolationMode.High;
            g.SmoothingMode = Primitives.SmoothingMode.AntiAlias;
            g.TextRenderingHint = Primitives.TextRenderingHint.AntiAlias;
            float BaseScale2 = 1.0f;

            // g.FillPath(new SolidBrush(Color.Teal), GP);
            TheSettings = Rend.GetHashSettings(Label, huerange);

            RenderIconBackdrop(g, TheSettings.BackGroundColor, TheSettings, x1, x2, y1, y2, 0, Rend);
            Rend.DrawTiling(TheSettings, M, G3, Color.FromRgba((byte)0, (byte)0, (byte)0, (byte)40), Color.Black, Math.Max(3, 4.5f * BaseScale2), false);
            Rend.DrawTiling(TheSettings, M, G3, TheSettings.BackgroundHighlight, Color.Black, Math.Max(1.4f, 3 * BaseScale2), false);
            Rend.DrawTiling(TheSettings, M, G3, Color.FromRgba((byte)255, (byte)255, (byte)0, (byte)100), Color.Black, Math.Max(1.0f, 1.4f * BaseScale2), false);

            // Composite TileGfx onto Output using ProcessPixelRows for performance
            Output.ProcessPixelRows(TileGfx, (outputAccessor, tileAccessor) =>
            {
                for (int y = 0; y < outputAccessor.Height; y++)
                {
                    Span<Rgba32> outputRow = outputAccessor.GetRowSpan(y);
                    Span<Rgba32> tileRow = tileAccessor.GetRowSpan(y);
                    
                    for (int x = 0; x < outputRow.Length; x++)
                    {
                        var c = Color.FromRgba(outputRow[x].R, outputRow[x].G, outputRow[x].B, outputRow[x].A);
                        if (outputRow[x].A > 0)
                        {
                            var tp = Color.FromRgba(tileRow[x].R, tileRow[x].G, tileRow[x].B, tileRow[x].A);
                            var c2 = InterpolateColor(c, tp, tileRow[x].A / 255.0f);
                            var c2Pixel = c2.ToPixel<Rgba32>();
                            outputRow[x] = new Rgba32(c2Pixel.R, c2Pixel.G, c2Pixel.B, outputRow[x].A);
                        }
                    }
                }
            });

            g.DrawPath(new Pen(Color.FromRgba((byte)0, (byte)0, (byte)0, (byte)60), 3), GP.ToImageSharpPath());
            g.FillPath(new SolidBrush(Color.FromRgba((byte)255, (byte)255, (byte)255, (byte)255)), GP.ToImageSharpPath());


            G.DrawImage(Output, 0, 0, w, h);

        }

        private static void RenderIconBackdrop(Graphics g, Color C, Settings TheSettings, float x1, float x2, float y1, float y2, int offset, TINRSArtWorkRenderer R)
        {
            // R.TD.Create(TheSettings.TileType); // Requires checking Tiling.cs changes checking if Create exists or what arguments it takes. Assuming no change needed here if other errors dont complain about this.
            R.TD.Create(TheSettings.TileType);
            var M = R.TD.NormalizeSize();
            var T = R.TD.DivisionSet[0];

            float w = x2 - x1;
            float h = y2 - y1;

            for (float s = 0.3f; s < .5; s += 0.05f)
            {

                List<PointF> P = new List<PointF>();
                P.Add(new PointF((T.A.x - M.x) * w * s + w / 2, (T.A.y - M.y) * h * s + h / 2));
                P.Add(new PointF((T.B.x - M.x) * w * s + w / 2, (T.B.y - M.y) * h * s + h / 2));
                P.Add(new PointF((T.C.x - M.x) * w * s + w / 2, (T.C.y - M.y) * h * s + h / 2));

                float ox = (float)TheSettings.Rand.NextDouble()* w/4;
                float oy = (float)TheSettings.Rand.NextDouble()* h/4;
                float baserotation = 360.0f * (float)TheSettings.Rand.NextDouble();
                for (int i = 0; i < 5; i++)
                {
                    Matrix Mm = new Matrix();
                    Mm.RotateAt(360.0f * baserotation + (360.0f*i)/5.0f, new PointF(w / 2, h / 2));
                    Mm.Translate(ox, oy);
                    var Pa = P.ToArray();
                    Mm.TransformPoints(Pa);
                    //g.FillPolygon(new SolidBrush(C), Pa);

                    Matrix Mm2 = new Matrix();
                    Mm2.RotateAt(360.0f * baserotation - (360.0f * i) / 5.0f, new PointF(w / 2, h / 2));
                    Mm2.Translate(ox, oy);
                    //Mm2.Scale(-1, 1);
                    var Pa2 = P.ToArray();
                    Mm2.TransformPoints(Pa2);
                    g.FillPolygon(new SolidBrush(C), Pa2);


                }
            }

            //            g.FillEllipse(new SolidBrush(C), new RectangleF(x1 + 7, y1 + 7 , x2 - x1, y2 - y1));
        }

        public static void SaveMultiIcon(string outputfile, string label, float huerange = -1)
        {
            //Bitmap B1 = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            //Graphics.FromImage(B1).Clear(Color.Transparent); ;
            //B1.MakeTransparent(Color.Transparent);

            //Artwork.TINRSArtWorkRenderer.DrawIcon(B1.Width, B1.Height, Graphics.FromImage(B1), label, huerange);

            Bitmap B2 = new Bitmap(32, 32);
            new ImageSharpGraphicsInterface(B2).Clear(Color.Transparent);
            Artwork.TINRSArtWorkRenderer.DrawIcon(B2.Width, B2.Height, new ImageSharpGraphicsInterface(B2), label, huerange);

            Bitmap B2b = new Bitmap(48, 48);
            new ImageSharpGraphicsInterface(B2b).Clear(Color.Transparent);
            Artwork.TINRSArtWorkRenderer.DrawIcon(B2b.Width, B2b.Height, new ImageSharpGraphicsInterface(B2b), label, huerange);


            Bitmap B3 = new Bitmap(64, 64);
            new ImageSharpGraphicsInterface(B3).Clear(Color.Transparent);
            Artwork.TINRSArtWorkRenderer.DrawIcon(B3.Width, B3.Height, new ImageSharpGraphicsInterface(B3), label, huerange);


            Bitmap B4 = new Bitmap(128, 128);
            new ImageSharpGraphicsInterface(B4).Clear(Color.Transparent);
            Artwork.TINRSArtWorkRenderer.DrawIcon(B4.Width, B4.Height, new ImageSharpGraphicsInterface(B4), label, huerange);

            List<Bitmap> IcoBMPs = new List<Bitmap>() { B2, B2b, B3, B4 };
            Artwork.TINRSArtWorkRenderer.SaveAsIcon(IcoBMPs, outputfile);
        }


        public void DrawTiling(Settings S, Bitmap MaskBitmap, Graphics G, Color FGColor, Color BGColor, float linewidth, bool Clear = true)
        {
            G.InterpolationMode = Primitives.GraphicsInterpolationMode.High;
            G.SmoothingMode = Primitives.SmoothingMode.AntiAlias;
            Color FG = FGColor;
            Color BG = BGColor;

            if (S.InvertOutput)
            {
                FG = BGColor;
                BG = FGColor;
            }
            if (S.Mode == Settings.ArtMode.QuadTree)
            {
                if (ArtTree != null)
                {
                    if (Clear) G.Clear(BG);
                    G.RotateTransform(S.DegreesOff);
                    // e.Graphics.TranslateTransform(0, -140);
                    ArtTree.DrawArt(G, FG);
                }
            }

            if (S.Mode == Settings.ArtMode.Delaunay)
            {
                Delaunay.Render(G, FG, BG);
            }

            if (S.Mode == Settings.ArtMode.Tiling )
            {
                if (Clear) G.Clear(BG);
                Pen P = new Pen(FG, linewidth);
                for (int j = 0; j < SubDivPoly.Count; j++)
                {
                    var a = SubDivPoly[j];
                    PointF[] ThePoints = new PointF[a.Vertices.Count];
                    for (int i = 0; i < a.Vertices.Count; i++)
                    {
                        ThePoints[i] = new PointF((float)a.Vertices[i].x,(float)a.Vertices[i].y);
                    }
                    G.DrawPolygon(P,  ThePoints);
                }
            }
        }

        public static Color GetHashColor(string text)
        {
            return MakeColor(HashHue(text));
        }

        public static Color GetHashHighlight(string text)
        {
            return MakeHighlight(HashHue(text));
        }

        public static Color MakeColor(double H)
        {
            int r, g, b;


//            GerberLibrary.MathHelpers.HsvToRgb(H, 1.0, 0.5, out r, out g, out b);
            return Helpers.Refraction(((float)H / 360.0f)*0.3f + 0.2f);
            
//            return Color.FromArgb(r, g, b);

        }
        public static Color MakeHighlight(double H)
        {
            double DH = ((H + 60) % 120) - 60;
            H += DH * 0.4;

            return Helpers.Refraction(((float)H / 360.0f) * 0.3f + 0.2f);


//            GerberLibrary.MathHelpers.HsvToRgb(H, 1.0, 0.7, out r, out g, out b);
  //          return Color.FromArgb(r, g, b);
        }

        private static double HashHue(string text)
        {
            double H = 0;
            for (int i = 0; i < text.Length; i++)
            {
                H += ((text[i] - 'A') % 26) * (360.0 / 26);
                H = H % 360;
            }

            return H;
        }

        public Settings GetHashSettings(string text, float huerange = -1)
        {
            Settings S = new Settings();
            S.MarcelPlating = false;

            S.BackGroundColor = GetHashColor(text);
            S.BackgroundHighlight = GetHashHighlight(text);
            if (huerange > -1)
            {
                S.BackGroundColor = MakeColor(huerange * 360.0f);
                S.BackgroundHighlight = MakeHighlight(huerange * 360.0f);
            }
            S.TileType = Tiling.TilingType.RegularTriangle;
            S.MaxSubDiv = 6;
            for (int i = 0; i < text.Length; i++)
            {
                S.DegreesOff += (int)(text[i] * 112.123);
                S.DegreesOff = S.DegreesOff % 360;
            }
            S.Rand = new Random((int)(S.DegreesOff * 100.0));

            return S;
        }

        public void BuildTree(Bitmap Mask, Settings TheSettings)
        {
            int i = Math.Max(Mask.Width, Mask.Height);
            int R = 1;

            while (R < i) R *= 2;

            MaskTree = new QuadTreeNode() { xstart = 0, ystart = 0, xend = R, yend = R };

            float ThresholdLevel = TheSettings.Threshold * 0.01f;


            // Use ImageSharp ProcessPixelRows for fast pixel access
            Mask.ProcessPixelRows(accessor =>
            {
                for (int yy = 0; yy < accessor.Height; yy++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(yy);
                    for (int xx = 0; xx < row.Length; xx++)
                    {
                        var pixel = row[xx];
                        Color C = Color.FromRgba(pixel.R, pixel.G, pixel.B, pixel.A);
                        bool doit = false;
                        if (TheSettings.InvertSource)
                        {
                            doit = C.GetBrightness() > ThresholdLevel;
                        }
                        else
                        {
                            doit = C.GetBrightness() < ThresholdLevel;
                        }
                        if (doit)
                        {
                            MaskTree.Insert(xx, yy, new SolidQuadTreeItem() { x = (int)xx, y = (int)yy }, 8);
                        }
                    }
                }
            });
            /*

            for (int x = 0; x < Mask.Width; x++)
            {
                for (int y = 0; y < Mask.Height; y++)
                {
                    var C = Mask.GetPixel(x, y);
                    bool doit = false;
                    if (TheSettings.InvertSource)
                    {
                        doit = C.GetBrightness() > ThresholdLevel;
                    }
                    else
                    {
                        doit = C.GetBrightness() < ThresholdLevel;
                    }
                    if (doit)
                    {
                        MaskTree.Insert(x, y, new SolidQuadTreeItem() { x = (int)x, y = (int)y }, 8);
                    }
                }
            }

    */
        }

        /// <summary>
        /// Builds artwork geometry from the mask using current settings.
        /// This overload maintains backward compatibility.
        /// </summary>
        public int BuildStuff(Bitmap aMask, Settings TheSettings)
        {
            return BuildStuff(aMask, TheSettings, CancellationToken.None, null);
        }

        /// <summary>
        /// Builds artwork geometry from the mask using current settings.
        /// Supports cancellation and progress reporting for background execution.
        /// </summary>
        /// <param name="aMask">The mask bitmap</param>
        /// <param name="TheSettings">Rendering settings</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="progress">Progress reporter for UI updates</param>
        /// <returns>Elapsed time in milliseconds</returns>
        public int BuildStuff(Bitmap aMask, Settings TheSettings, CancellationToken cancellationToken, IProgress<RenderProgress> progress)
        {
            DirectBitmap Mask = new DirectBitmap(aMask.Width, aMask.Height);
            // Copy the input bitmap to the DirectBitmap
            aMask.ProcessPixelRows(Mask.Image, (sourceAccessor, destAccessor) =>
            {
                for (int y = 0; y < sourceAccessor.Height; y++)
                {
                    sourceAccessor.GetRowSpan(y).CopyTo(destAccessor.GetRowSpan(y));
                }
            });
            

            int i = Math.Max(Mask.Width, Mask.Height);
            int R = 1;
            while (R < i) R *= 2;
            ArtTree = null;
            float ThresholdLevel = TheSettings.Threshold * 0.01f;
            switch (TheSettings.Mode)
            {
                case Settings.ArtMode.QuadTree:
                    {
                        DateTime rR = DateTime.Now;

                        ArtTree = new QuadTreeNode() { xstart = -1000, ystart = -1000, xend = R, yend = R };
                        float hoek = (float)((6.283 * TheSettings.DegreesOff) / 360.0);
                        int progressInterval = Math.Max(1, Mask.Width / 20); // Report every 5%
                        progress?.Report(new RenderProgress(0, "Building QuadTree..."));
                        
                        for (int x = 0; x < Mask.Width; x++)
                        {
                            // Check for cancellation at each column
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            // Report progress every 5%
                            if (x % progressInterval == 0)
                            {
                                int percent = (x * 100) / Mask.Width;
                                progress?.Report(new RenderProgress(percent, $"Building QuadTree ({percent}%)..."));
                            }
                            
                            for (int y = 0; y < Mask.Height; y++)
                            {
                                var C = Mask.GetPixelFast(x, y);
                                bool doit = false;
                                if (TheSettings.InvertSource)
                                {
                                    doit = C.GetBrightness() > ThresholdLevel;
                                }
                                else
                                {
                                    doit = C.GetBrightness() < ThresholdLevel;
                                }
                                if (doit)
                                {
                                    double cx = Math.Cos(hoek) * x + Math.Sin(hoek) * y;
                                    double cy = Math.Sin(hoek) * -x + Math.Cos(hoek) * y;
                                    ArtTree.Insert((int)cx, (int)cy, new SolidQuadTreeItem() { x = (int)cx, y = (int)cy }, TheSettings.MaxSubDiv);
                                }
                            }
                        }
                        progress?.Report(new RenderProgress(100, "QuadTree complete") { IsComplete = true });
                        var Elapsed = DateTime.Now - rR;
                        return (int)Elapsed.TotalMilliseconds;
                    }

                case Settings.ArtMode.Delaunay:
                    {
                        DateTime rR = DateTime.Now;
                        ArtTree = new QuadTreeNode() { xstart = -1000, ystart = -1000, xend = R, yend = R };
                        float hoek = (float)((6.283 * TheSettings.DegreesOff) / 360.0);
                        int progressInterval = Math.Max(1, Mask.Width / 20);
                        progress?.Report(new RenderProgress(0, "Building Delaunay..."));
                        
                        for (int x = 0; x < Mask.Width; x++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            if (x % progressInterval == 0)
                            {
                                int percent = (x * 80) / Mask.Width; // 0-80% for pixel processing
                                progress?.Report(new RenderProgress(percent, $"Building Delaunay ({percent}%)..."));
                            }
                            
                            for (int y = 0; y < Mask.Height; y++)
                            {
                                var C = Mask.GetPixelFast(x, y);
                                bool doit = false;
                                if (TheSettings.InvertSource)
                                {
                                    doit = C.GetBrightness() > ThresholdLevel;
                                }
                                else
                                {
                                    doit = C.GetBrightness() < ThresholdLevel;
                                }
                                if (doit)
                                {
                                    double cx = Math.Cos(hoek) * x + Math.Sin(hoek) * y;
                                    double cy = Math.Sin(hoek) * -x + Math.Cos(hoek) * y;
                                    ArtTree.Insert((int)cx, (int)cy, new SolidQuadTreeItem() { x = (int)cx, y = (int)cy }, TheSettings.MaxSubDiv);
                                }
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Report(new RenderProgress(85, "Computing Delaunay triangulation..."));
                        Delaunay.Build(ArtTree, TheSettings.DegreesOff);

                        progress?.Report(new RenderProgress(100, "Delaunay complete") { IsComplete = true });
                        var Elapsed = DateTime.Now - rR;
                        return (int)Elapsed.TotalMilliseconds;
                    };

                case Settings.ArtMode.Tiling:
                    {
                        progress?.Report(new RenderProgress(0, "Creating tiling..."));
                        TD.Create(TheSettings.TileType);
                        var P = TD.CreateBaseTriangle(TheSettings.BaseTile, 1000);
                        var P2 = TD.CreateBaseTriangle(TheSettings.BaseTile, 1000);
                        P.Rotate(TheSettings.DegreesOff);
                        P.AlterToFit(Mask.Width, Mask.Height);
                        P2.Rotate(TheSettings.DegreesOff);
                        P2.AlterToFit(Mask.Width, Mask.Height);

                        if (TheSettings.Symmetry)
                        {
                            P.ShiftToEdge(Mask.Width / 2, Mask.Height / 2);
                            P2.ShiftToEdge(Mask.Width / 2, Mask.Height / 2);
                            P2.Flip(Mask.Width / 2, Mask.Height / 2);
                            if (TheSettings.SuperSymmetry)
                            {
                                P2.MirrorAround(Mask.Width / 2, Mask.Height / 2);
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Report(new RenderProgress(10, "Subdividing polygons..."));
                        
                        DateTime rR = DateTime.Now;
                        SubDivPoly = TD.SubdivideAdaptive(P, TheSettings.MaxSubDiv, MaskTree, TheSettings.alwayssubdivide, cancellationToken);

                        if (TheSettings.Symmetry)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            progress?.Report(new RenderProgress(30, "Subdividing symmetry..."));
                            SubDivPoly.AddRange(TD.SubdivideAdaptive(P2, TheSettings.MaxSubDiv, MaskTree, TheSettings.alwayssubdivide, cancellationToken));
                        }
                        
                        // Report intermediate polygons for progressive rendering
                        progress?.Report(new RenderProgress(50, "Subdivision complete") { IntermediatePolygons = SubDivPoly.ToList() });

                        if (TheSettings.xscalesmallerlevel != 0)
                        {
                            float midx = Mask.Width / 2.0f;
                            float width = Mask.Width;
                            float offs = TheSettings.xscalecenter * 0.01f * width;
                            foreach (var A in SubDivPoly)
                            {
                                var M = A.Mid();
                                float scaler = 1.0f - ((float)(M.x - offs) / width) * TheSettings.xscalesmallerlevel * 0.01f;
                                //scaler = Math.Max(0, Math.Min(1.0f, scaler));
                                A.ScaleDown(TheSettings.scalingMode, scaler);
                            }
                        }
                        if (TheSettings.scalesmallerfactor != 1.0f)
                        {
                            foreach (var A in SubDivPoly)
                            {
                                A.ScaleDown(Settings.TriangleScaleMode.Balanced, TheSettings.scalesmallerfactor);
                            }
                        }

                        if (TheSettings.scalesmaller != 0)
                        {
                            float scaler = Math.Abs(TheSettings.scalesmaller);
                            if (TheSettings.scalesmaller > 0)
                            {
                                scaler = scaler / 10.0f;
                            }
                            else
                            {
                                scaler = -scaler / 10.0f;
                            }
                            foreach (var A in SubDivPoly)
                            {

                                if (A.depth - TheSettings.scalesmallerlevel <= 1)
                                {

                                }
                                else
                                {
                                    A.ScaleDown(TheSettings.scalingMode, (1 + scaler * (1.0f / (A.depth - TheSettings.scalesmallerlevel))));

                                }
                            }
                        }
                        if (TheSettings.distanceToMaskScale != 0)
                        {
                            float scaler = Math.Abs(TheSettings.distanceToMaskScale);
                            if (TheSettings.distanceToMaskScale > 0)
                            {
                                scaler = scaler / 10.0f;
                            }
                            else
                            {
                                scaler = -scaler / 10.0f;
                            }


                            float aThresholdLevel = TheSettings.Threshold * 0.01f;
                            if (TheSettings.DistanceMaskFile.Length > 0)
                            {
                                Bitmap B = Image.Load<Rgba32>(TheSettings.DistanceMaskFile);

                                DirectBitmap DMask = new DirectBitmap(aMask.Width, aMask.Height);
                                // Resize and copy B into DMask
                                B.Mutate(x => x.Resize(aMask.Width, aMask.Height));
                                B.ProcessPixelRows(DMask.Image, (sourceAccessor, destAccessor) =>
                                {
                                    for (int y = 0; y < Math.Min(sourceAccessor.Height, destAccessor.Height); y++)
                                    {
                                        sourceAccessor.GetRowSpan(y).CopyTo(destAccessor.GetRowSpan(y));
                                    }
                                });

                                foreach (var A in SubDivPoly)
                                {

                                    var m = A.Mid();
                                    float sum = GetPixelSum(m, DMask, TheSettings.distanceToMaskRange, aThresholdLevel, TheSettings.InvertSource);
                                    //if (sum > 1) sum = 1;
                                    A.ScaleDown(TheSettings.scalingMode, (scaler * sum));


                                }

                            }
                            else
                            {
                                foreach (var A in SubDivPoly)
                                {

                                    var m = A.Mid();
                                    float sum = GetPixelSum(m, Mask, TheSettings.distanceToMaskRange, aThresholdLevel, TheSettings.InvertSource);
                                    //if (sum > 1) sum = 1;
                                    A.ScaleDown(TheSettings.scalingMode, (scaler * sum));


                                }

                            }
                        }


                        if (TheSettings.MarcelPlating)
                        {
                            List<Tiling.Polygon> MarcelShapes = new List<Tiling.Polygon>();
                            foreach (var A in SubDivPoly)
                            {


                                MarcelShape MS = new MarcelShape();
                                MarcelShape MS2 = new MarcelShape();

                                foreach (var v in A.Vertices)
                                {
                                    MS2.Vertices.Add(new ClipperLib.IntPoint((long)((v.x+1000)*1000), (long)((v.y+1000)*1000)));
                                }

                                MS.ShrinkFromShape(MS2.Vertices, TheSettings.Gap /2 + TheSettings.Rounding/2 );
                                Paths Ps = new Paths();

                                Ps.AddRange(MS.BuildOutlines(TheSettings.Rounding / 2.0f));
                                if (TheSettings.BallRadius > 0) Ps.AddRange(MS.BuildHoles(TheSettings.BallRadius));

                                foreach(var p in Ps)
                                {
                                    Tiling.Polygon Poly = new Tiling.Polygon();
                                    Poly.Vertices.AddRange(from a in p select new vec2((a.X) * 0.001f-1000, (a.Y ) * 0.001f-1000));
                                    MarcelShapes.Add(Poly);
                                }


                            }
                            SubDivPoly.Clear();
                            SubDivPoly = MarcelShapes;
                        }

                        var Elapsed = DateTime.Now - rR;
                        return (int)Elapsed.TotalMilliseconds;
                    }

          
            };
            return 0;
        }



        private float GetPixelSum(vec2 m, DirectBitmap mask, float distanceToMaskRange, float ThresholdLevel, bool invert)
        {
            float sum = 0;
            if (distanceToMaskRange == 0) distanceToMaskRange = 0.001f;
            float wrange = distanceToMaskRange * mask.Width * 0.5f;
            float hrange = distanceToMaskRange * mask.Width * 0.5f;
            float total = 0;
            float[] cp = new float[40];
            float[] sp = new float[40];
            for (int p = 0; p < 40; p++)

            {
                double P = (p * Math.PI * 2) / 40.0f;
                sp[p] = (float)Math.Sin(P) * wrange;
                cp[p] = (float)Math.Cos(P) * wrange ;
            }

            for (int ring = 1;ring<10;ring++)
            {
                float RW = ring / 10.0f;
                for (int p = 0; p < 40; p++)
                    
                {
                    int x = (int)(cp[p] * RW + m.x);
                    int y = (int)(sp[p] * RW + m.y);
                    total++;
                    if (x >= 0 && x < mask.Width)
                    {
                        if (y >= 0 && y < mask.Height)
                        {
                            var C = mask.GetPixelFast(x, y);
                            float br = C.GetBrightness();
                            bool doit = false;
                            if (invert)
                            {
                                doit = br > ThresholdLevel;
                            }
                            else
                            {
                                doit = C.GetBrightness() < ThresholdLevel;
                            }
                            if (doit)
                            {
                                sum++;
                            }

                        }
                    }
                }

            }
            
            return 1000*sum / total;
        }
    }

}
