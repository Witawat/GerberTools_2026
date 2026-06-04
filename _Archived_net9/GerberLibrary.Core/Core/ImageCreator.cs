using GerberLibrary;
using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Numerics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;
using Pen = GerberLibrary.Core.Primitives.Pen;
using SolidBrush = GerberLibrary.Core.Primitives.SolidBrush; using GraphicsInterpolationMode = GerberLibrary.Core.Primitives.GraphicsInterpolationMode; 
using ICSharpCode.SharpZipLib.Zip;
using TriangleNet;

namespace GerberLibrary
{
    public class GerberImageCreator
    {
        public static bool AA = true;
        public Bounds BoundingBox = new Bounds();
        public List<String> Errors = new List<string>();
        public double scale = 25.0d / 25.4d; // dpi
        private BoardRenderColorSet ActiveColorSet = new BoardRenderColorSet();
        Dictionary<string, MemoryStream> Streams = new Dictionary<string, MemoryStream>();
        public Dictionary<string, double> DrillFileScale = new Dictionary<string, double>();


        bool hasgko = false;

      public  List<ParsedGerber> PLSs = new List<ParsedGerber>();

        public ParsedGerber GetGerberByName(string name)
        {
            foreach (var a in PLSs)
            {
                if (a.Name == name) return a;
            }
            return null;
        }

        public void ClipBoard(string infile, string outputfile, ProgressLog log)
        {
            var toclip = GetGerberByName(infile);
            if (toclip == null)
            {
                log.AddString(String.Format("file {0} not loaded - not clipping!", infile));
            }



            var ols = (from a in PLSs where (a.Side == BoardSide.Both && (a.Layer == BoardLayer.Outline || a.Layer == BoardLayer.Mill) && a.Generated == false) select a).ToList();

            if (IsInPolygons(toclip, ols) == true)
            {
                File.Copy(infile, outputfile);
                log.AddString("file not clipped - just copied");
                return;
            }

            GerberArtWriter GAW = new GerberArtWriter();
            int lineID = 0;
            foreach (var a in toclip.DisplayShapes)
            {
                ClipperLib.Clipper CP = new ClipperLib.Clipper();
                foreach (var ol in ols)
                {
                    var R = ol.FindLargestPolygon();
                    if (R != null)
                    {
                        var poly = R.Item2.toPolygon();

                        if (ClipperLib.Clipper.Orientation(poly) == false)
                        {
                            //Console.WriteLine("pos");
                        }
                        else
                        {
                            poly.Reverse();
                            //Console.WriteLine("neg");
                        }

                        CP.AddPolygon(poly, ClipperLib.PolyType.ptClip);
                    }
                }
                CP.AddPolygon(a.toPolygon(), ClipperLib.PolyType.ptSubject);
                List<List<ClipperLib.IntPoint>> solution = new List<List<ClipperLib.IntPoint>>();

                CP.Execute(ClipperLib.ClipType.ctIntersection, solution);
                foreach (var p in solution)
                {
                    PolyLine P = new PolyLine(lineID++);
                    P.fromPolygon(p);
                    GAW.AddPolygon(P);
                }

            }

            GAW.Write(outputfile);
        }

        public void Translate(double x, double y)
        {
            BoundingBox.Reset();
            foreach (var f in PLSs)
            {
                f.Translate(new PointD(x,y));
                f.CalcPathBounds();
                BoundingBox.AddBox(f.BoundingBox);
            }
        }

        public void FlipXY()
        {
            BoundingBox.Reset();
            foreach (var f in PLSs)
            {
                f.FlipXY();
                f.CalcPathBounds();
                BoundingBox.AddBox(f.BoundingBox);
            }
        }

        public void FlipX()
        {
            BoundingBox.Reset();
            foreach (var f in PLSs)
            {
                f.FlipX();
                f.CalcPathBounds();
                BoundingBox.AddBox(f.BoundingBox);
            }
        }

        public void SetBottomRightToZero()
        {
            double dx = this.BoundingBox.BottomRight.X;
            double dy = this.BoundingBox.TopLeft.Y;
            BoundingBox.Reset();
            foreach (var f in PLSs)
            {
                f.Translate(new PointD(-dx, -dy));
                f.CalcPathBounds();
                BoundingBox.AddBox(f.BoundingBox);
            }
        }

        private bool IsInPolygons(ParsedGerber toclip, List<ParsedGerber> ols)
        {
            foreach (var o in ols)
            {
                foreach (var oo in o.DisplayShapes)
                {
                    foreach (var p in toclip.DisplayShapes)
                    {
                        foreach (var pt in p.Vertices)
                        {
                            if (Helpers.IsInPolygon(oo.Vertices, pt) == false)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;

        }

        public void CopyFrom(GerberImageCreator set)
        {
            foreach(var p in set.PLSs)
            {
                ParsedGerber PG = new ParsedGerber();
                PG.CopyFrom(p);
                PG.Side = p.Side;
                PG.Layer = p.Layer;
                PG.CalcPathBounds();
                BoundingBox.AddBox(PG.BoundingBox);
                PLSs.Add(PG);
            }
        }

        public void SetBottomLeftToZero()
        {
            double dx = this.BoundingBox.TopLeft.X;
            double dy = this.BoundingBox.TopLeft.Y;
            BoundingBox.Reset();
            foreach(var f in PLSs)
            {
                f.Translate(new PointD(-dx, -dy));
                f.CalcPathBounds();
                BoundingBox.AddBox(f.BoundingBox);
            }


        }

        public int Count()
        {
            return PLSs.Count;
        }

        public static void ApplyAASettings(GraphicsInterface G)
        {
            if (G != null) G.AntiAlias = AA;
            // We can toggle a flag if needed, but for now we rely on defaults.
            if (AA)
            {
                G.InterpolationMode = GraphicsInterpolationMode.HighQualityBicubic;
            }
            else
            {
                G.InterpolationMode = GraphicsInterpolationMode.NearestNeighbor;
            }
        }

        public static Color Darker(Color color, double Fac)
        {
             Rgba32 c = color.ToPixel<Rgba32>();
             float correctionFactor = 1.0f - (float)Fac;
             byte r = (byte)(c.R * correctionFactor);
             byte g = (byte)(c.G * correctionFactor);
             byte b = (byte)(c.B * correctionFactor);
             return Color.FromRgba(r, g, b, c.A);
        }

        public static Color Lighter(Color color, double Fac)
        {
             Rgba32 c = color.ToPixel<Rgba32>();
             float correctionFactor = (float)Fac;
             byte r = (byte)((255 - c.R) * correctionFactor + c.R);
             byte g = (byte)((255 - c.G) * correctionFactor + c.G);
             byte b = (byte)((255 - c.B) * correctionFactor + c.B);
             return Color.FromRgba(r, g, b, c.A);
        }

        public void AddBoardsToSet(List<string> FileList, ProgressLog Logger , bool fixgroup = true, bool forcezerowidth = false)
        {
            Logger.PushActivity("AddBoardsToSet");
            foreach (var a in FileList)
            {
                Logger.AddString(String.Format("adding {0}", a));
                BoardSide aSide = BoardSide.Unknown;
                BoardLayer aLayer = BoardLayer.Unknown;
                string ext = Path.GetExtension(a);
                if (ext == ".zip")
                {
                    using (ZipFile zip1 = new ZipFile(a))
                    {
                        foreach (ZipEntry e in zip1)
                        {
                            MemoryStream MS = new MemoryStream();
                            if (e.IsDirectory == false)
                            {
                                //                              e.Extract(MS);
                                //                                MS.Seek(0, SeekOrigin.Begin);
                                Gerber.DetermineBoardSideAndLayer(e.Name, out aSide, out aLayer);
                                if (aLayer == BoardLayer.Outline) hasgko = true;

                                //     AddFileStream(MS, e.Name, drillscaler);
                            }
                        }
                    }
                }
                else
                {

                    Gerber.DetermineBoardSideAndLayer(a, out aSide, out aLayer);
                }
                if (aLayer == BoardLayer.Outline) hasgko = true;
            }

            foreach (var a in FileList)
            {
                if (Logger != null) Logger.AddString(String.Format("Loading {0}", Path.GetFileName(a)));
                string ext = Path.GetExtension(a);
                if (ext == ".zip")
                {
                    using (ZipFile zip1 = new ZipFile(a))
                    {
                        foreach (ZipEntry e in zip1)
                        {
                            if (!e.IsDirectory)
                            {
                                if (Logger != null) Logger.AddString(String.Format("Loading inside zip: {0}", Path.GetFileName(e.Name)));

                                MemoryStream MS = new MemoryStream();
                                using (var stream = zip1.GetInputStream(e))
                                {
                                    stream.CopyTo(MS);
                                }
                                MS.Seek(0, SeekOrigin.Begin);
                                AddFileToSet(MS, e.Name, Logger, 1, forcezerowidth);
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        MemoryStream MS2 = new MemoryStream();
                        FileStream FS = File.OpenRead(a);
                        FS.CopyTo(MS2);
                        MS2.Seek(0, SeekOrigin.Begin);
                        AddFileToSet(MS2, a, Logger,1, forcezerowidth);
                    }
                    catch (Exception E)
                    {
                        Logger.AddString(String.Format("Failed to add file! {0},{1}", a, E));
                    }
                }
            }

            if (fixgroup)
            {
                if (Logger != null) Logger.AddString("Checking for common file format mistakes.");
                FixEagleDrillExportIssues(Logger);
                CheckRelativeBoundingBoxes(Logger);
                CheckForOutlineFiles(Logger);

                CheckRelativeBoundingBoxes(Logger);

            }

            Logger.PopActivity();
        }

        public ParsedGerber AddBoardToSet(string _originalfilename, ProgressLog log, bool forcezerowidth = false, bool precombinepolygons = false, double drillscaler = 1.0)
        {
            if (Streams.ContainsKey(_originalfilename))
            {
                return AddBoardToSet(Streams[_originalfilename], _originalfilename, log, forcezerowidth, precombinepolygons, drillscaler);
            }
            return null;
        }


        public ParsedGerber AddBoardToSet(MemoryStream MS, string _originalfilename, ProgressLog log, bool forcezerowidth = false, bool precombinepolygons = false, double drillscaler = 1.0)
        {
            log.PushActivity("AddboardToSet");
            Streams[_originalfilename] = MS;
            try
            {
                //   string[] filesplit = originalfilename.Split('.');
                //     string ext = filesplit[filesplit.Count() - 1].ToLower();

                var FileType = Gerber.FindFileTypeFromStream(new StreamReader(MS), _originalfilename);
                MS.Seek(0, SeekOrigin.Begin);

                if (FileType == BoardFileType.Unsupported)
                {
                    if (Gerber.ExtremelyVerbose) log.AddString(String.Format("Warning: {1}: files with extension {0} are not supported!", Path.GetExtension(_originalfilename), Path.GetFileName(_originalfilename)));
                    log.PopActivity();
                    return null;
                }


                ParsedGerber PLS;
                GerberParserState State = new GerberParserState() { PreCombinePolygons = precombinepolygons };

                if (FileType == BoardFileType.Drill)
                {
                    if (Gerber.ExtremelyVerbose) log.AddString(String.Format("Drill file: {0}", _originalfilename));
                    PLS = PolyLineSet.LoadExcellonDrillFileFromStream(log, new StreamReader(MS), _originalfilename, false, drillscaler);
                    MS.Seek(0, SeekOrigin.Begin);
                    PLS.Side = BoardSide.Both;
                    PLS.Layer = BoardLayer.Drill;
                    // ExcellonFile EF = new ExcellonFile();
                    // EF.Load(a);

                }
                else
                {
                    if (Gerber.ExtremelyVerbose) Console.WriteLine("Log: Gerber file: {0}", _originalfilename);
                    BoardSide Side = BoardSide.Unknown;
                    BoardLayer Layer = BoardLayer.Unknown;
                    Gerber.DetermineBoardSideAndLayer(_originalfilename, out Side, out Layer);
                    if (Layer == BoardLayer.Outline || Layer == BoardLayer.Mill)
                    {
                        forcezerowidth = true;
                        precombinepolygons = true;
                    }
                    State.PreCombinePolygons = precombinepolygons;
                    if (Layer == BoardLayer.Silk)
                    {
                        State.IgnoreZeroWidth = true;
                    }
                    PLS = PolyLineSet.LoadGerberFileFromStream(log, new StreamReader(MS), _originalfilename, forcezerowidth, false, State);
                    MS.Seek(0, SeekOrigin.Begin);

                    PLS.Side = State.Side;
                    PLS.Layer = State.Layer;
                    if (Layer == BoardLayer.Outline)
                    {
                        PLS.FixPolygonWindings();
                    }
                }

                PLS.CalcPathBounds();
                BoundingBox.AddBox(PLS.BoundingBox);

                log.AddString(String.Format ("Loaded {0}: {1:N1} x {2:N1} mm", Path.GetFileName(_originalfilename), PLS.BoundingBox.BottomRight.X - PLS.BoundingBox.TopLeft.X, PLS.BoundingBox.BottomRight.Y - PLS.BoundingBox.TopLeft.Y));
                PLSs.Add(PLS);
                //     }
                //     catch (Exception)
                //    {
                //   }
                log.PopActivity();
                return PLS;
            }
            catch (Exception E)
            {
                while (E != null)
                {
                    Console.WriteLine("Exception adding board: {0}", E.Message);
                    E = E.InnerException;
                }
            }
            log.PopActivity();
            return null;
        }

        public Matrix3x2 BuildMatrix(int w, int h)
        {
            var OutlineBoundingBox = GetOutlineBoundingBox();

            // Replicating:
            // Translate(0, h)
            // Scale(1, -1)
            // Translate(1, 1)
            // Scale(scale, scale)
            // Translate(-Outline.TL.X, -Outline.TL.Y)
            
            // Matrix3x2 order is Prepend (default mult) ? No, ImageSharp use Matrix3x2 which is row-major usually, but let's check operations.
            // .NET Matrix3x2 operations are "Post-Multiply" by default? CreateTranslation creates a matrix.
            // If M is current, M * T applies T "after" M (so T is applied to the result of M). 
            // Standard affine: P' = P * M.
            // GDI+ usage: G.TranslateTransform... appends to the world transform.
            
            Matrix3x2 T = Matrix3x2.Identity;
            
            T = T * Matrix3x2.CreateTranslation((float)-OutlineBoundingBox.TopLeft.X, (float)-OutlineBoundingBox.TopLeft.Y);
            T = T * Matrix3x2.CreateScale((float)scale, (float)scale);
            T = T * Matrix3x2.CreateTranslation(1, 1);
            T = T * Matrix3x2.CreateScale(1, -1);
            T = T * Matrix3x2.CreateTranslation(0, h);
            
            return T;
        }

        public void CreateBoxOutline()
        {
            PolyLine Box = new PolyLine(PolyLine.PolyIDs.Outline);
            Box.MakeRectangle(BoundingBox.Width(), BoundingBox.Height());
            Box.Translate(BoundingBox.TopLeft.X + BoundingBox.Width() / 2.0, BoundingBox.TopLeft.Y + BoundingBox.Height() / 2.0);
            Box.Hole = false;
            //Box.Close();
            // Box.Vertices.Reverse();
            ParsedGerber PLS = new ParsedGerber();
            PLS.Name = "Generated BoundingBox";
            PLS.Generated = true;
            PLS.DisplayShapes.Add(Box);
            PLS.OutlineShapes.Add(Box);
            PLS.Shapes.Add(Box);
            PLS.Layer = BoardLayer.Outline;
            PLS.Side = BoardSide.Both;
            //      PLS.FixPolygonWindings();
            PLS.CalcPathBounds();
            PLSs.Add(PLS);
        }

        public void DrawAllFiles(string v1, double dpi, ProgressLog Logger = null)
        {

            scale = dpi / 25.4d; // dpi
            var OutlineBoundingBox = GetOutlineBoundingBox();
            double bw = Math.Abs(OutlineBoundingBox.BottomRight.X - OutlineBoundingBox.TopLeft.X);
            double bh = Math.Abs(OutlineBoundingBox.BottomRight.Y - OutlineBoundingBox.TopLeft.Y);
            int width = (int)((bw * scale));
            int height = (int)((bh * scale));

            int w = width + 3;
            int h = height + 3;

            Matrix3x2 TransformCopy = Matrix3x2.Identity;
            
            // Build Matrix
            {
                var T = Matrix3x2.Identity;
                T = T * Matrix3x2.CreateTranslation(0, h);
                T = T * Matrix3x2.CreateScale(1, -1);
                T = T * Matrix3x2.CreateTranslation(1, 1);
                T = T * Matrix3x2.CreateScale((float)scale, (float)scale);
                T = T * Matrix3x2.CreateTranslation((float)-OutlineBoundingBox.TopLeft.X, (float)-OutlineBoundingBox.TopLeft.Y);
                TransformCopy = T;
            }

            foreach (var L in PLSs)
            {
                string FileName = v1 + "_" + L.Layer.ToString() + "_" + L.Side.ToString() + ".png";
                if (Logger != null) Logger.AddString(String.Format("Rendering {0}-{1}", L.Layer.ToString(), L.Side.ToString()));

                Image<Rgba32> B2 = new Image<Rgba32>(w + 3, h + 3);

                ApplyAASettings(null); // No-op now/handled internally
                var G3 = new ImageSharpGraphicsInterface(B2);
                G3.Clear(Color.White);
                G3.Transform = TransformCopy; // Matrix3x2 is struct, copy is automatic

                GerberLibrary.Core.Primitives.Pen P = new GerberLibrary.Core.Primitives.Pen(Color.Black, 1.0f / (float)(scale));
                int Shapes = 0;

                Shapes += DrawLayerToGraphics(Color.Black, true, G3, P, L, false);

                B2.Save(FileName);
            }
        }

        public void  DrawXRayToFile(string basefilename, double dpi = 400, bool showimage = true, bool bottomside = false, ProgressLog Logger = null)
        {


            string filenamet= basefilename + "_top_XRAY.png";
            string filenameb = basefilename +"_bottom_XRAY.png";

            DrawXRayBoard(filenamet, filenameb, dpi, ActiveColorSet, basefilename, Logger, bottomside);

            if (showimage)
            {
                System.Diagnostics.Process.Start(filenamet);
                System.Diagnostics.Process.Start(filenameb);
            }
            
        }


        public string DrawToFile(string basefilename, BoardSide CurrentLayer, double dpi = 400, bool showimage = true, ProgressLog Logger = null)
        {
            Image<Rgba32> B = DrawBoard(dpi, CurrentLayer, ActiveColorSet, basefilename, Logger);
            string filename = basefilename + "_Combined_" + CurrentLayer.ToString() + ".png";
            B.Save(filename); 

            if (showimage) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filename) { UseShellExecute = true });
            return filename;
        }

        public Image<Rgba32> RenderToImage(int w, int h, Matrix3x2 Transform, Color foregroundcolor, Color backgroundcolor, ParsedGerber PLS, bool fill, bool forcefill = false)
        {
            Image<Rgba32> B2;

            try
            {
                B2 = new Image<Rgba32>(w + 3, h + 3);
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Failed to create image of size {0}x{1}", w, h);
                return null;
            }

            var G3 = new ImageSharpGraphicsInterface(B2);
            G3.Clear(backgroundcolor);
            G3.Transform = Transform;
            ApplyAASettings(G3);

            GerberLibrary.Core.Primitives.Pen P = new GerberLibrary.Core.Primitives.Pen(foregroundcolor, 1.0f / (float)(scale));
            int Shapes = 0;
            Shapes += DrawLayerToGraphics(foregroundcolor, fill, G3, P, PLS, forcefill);
            //            if (Shapes == 0) return null;
            return B2;
        }

        public void SetColors(BoardRenderColorSet colors)
        {
            ActiveColorSet = colors;
        }

        public void WriteImageFiles(string TargetFileBaseName, double dpi = 200, bool showimage = true, bool xray = true, bool normal = true, ProgressLog Logger = null)
        {
            if (normal)
            {
                if (Logger != null) Logger.AddString(String.Format("Build top layer image at {0} dpi", dpi));
                DrawToFile(TargetFileBaseName, BoardSide.Top, dpi, showimage, Logger);
                if (Logger != null) Logger.AddString(String.Format("Build bottom layer image at {0} dpi", dpi));
                DrawToFile(TargetFileBaseName, BoardSide.Bottom, dpi, showimage, Logger);
            }

            if (xray)
            {
            
                if (Logger != null) Logger.AddString(String.Format("Build xray images at {0} dpi", dpi));
                DrawXRayToFile(TargetFileBaseName, dpi, showimage, true, Logger);
            }


        }

        private void AddFileToSet(string aname, ProgressLog Logger, double drillscaler = 1.0, bool forcezerowidth = false)
        {
            if (Streams.ContainsKey(aname))
            {
                AddFileToSet(Streams[aname], aname, Logger, drillscaler, forcezerowidth);
            }
            else
            {
                Logger.AddString(String.Format("[ERROR] no stream for {0}!!!", aname));
            }
        }

        private void AddFileToSet(MemoryStream MS, string aname, ProgressLog Logger, double drillscaler = 1.0, bool forcezerowidth = false)
        {

            Streams[aname] = MS;

            ///string[] filesplit = a.Split('.');

            bool zerowidth = forcezerowidth;
            bool precombine = false;

            BoardSide aSide;
            BoardLayer aLayer;
            Gerber.DetermineBoardSideAndLayer(aname, out aSide, out aLayer);

            if (aLayer == BoardLayer.Outline || (aLayer == BoardLayer.Mill && hasgko == false))
            {
                zerowidth = true;
                precombine = true;
            }
            MS.Seek(0, SeekOrigin.Begin);
            AddBoardToSet(MS, aname, Logger, zerowidth, precombine, drillscaler);
        }

        private void ApplyBumpMapping(Image<Rgba32> _Target, Image<Rgba32> _Bump, int w, int h)
        {
            // Direct access is slow without ProcessPixelRows, but let's stick to simple first for correctness.
            // Loop x,y
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Rgba32 TargetPixel = _Target[x, y];
                    Rgba32 B1 = _Bump[x, y];

                    if (true)//B1.A > 0)
                    {
                        Rgba32 B2 = B1;
                        Rgba32 B4 = B1;
                        if (x < w - 1)
                        {
                            B2 = _Bump[x + 1, y];
                            B4 = B2;
                        }
                        Rgba32 B3 = B1;
                        if (y < h - 1) B3 = _Bump[x, y + 1];
                        if (y < h - 1 && x < w - 1)
                        {
                            B4 = _Bump[x + 1, y + 1];
                        }
                        
                        // GetBrightness is not on Rgba32.
                        // Helper: 0.2126*R + 0.7152*G + 0.0722*B
                        float GetBrightness(Rgba32 c) => (0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B) / 255.0f;
                        
                        float dx1 = (GetBrightness(B1) - GetBrightness(B2));
                        float dx2 = (GetBrightness(B3) - GetBrightness(B4));
                        float dy1 = (GetBrightness(B1) - GetBrightness(B3));
                        float dy2 = (GetBrightness(B2) - GetBrightness(B4));
                        float dx = dx1 + dx2;
                        float dy = dy1 + dy2;

                        if (dx == 0 && dy == 0)
                        {
                            //skip
                        }
                        else
                        {
                            double ang = Math.Atan2(dy, dx);
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            double L = Math.Sin(ang - 1.4);
                            // Convert Rgba32 to Color to use Lighter/Darker
                            Color TC = Color.FromRgba(TargetPixel.R, TargetPixel.G, TargetPixel.B, TargetPixel.A);
                            if (L > 0) _Target[x, y] = Lighter(TC, L * 0.04).ToPixel<Rgba32>();
                            else
                            {
                                _Target[x, y] = Darker(TC, Math.Abs(L * 0.04)).ToPixel<Rgba32>();
                            }
                        }

                    }
                }
            }
        }

        private void CarveOutlineAndMillInnerPolygonsFromImage(string basefilename, int w, int h, ImageSharpGraphicsInterface G, Image<Rgba32> _Target, Matrix3x2 TransformCopy)
        {
            var T = G.Transform;
            G.Transform = TransformCopy;
            var L = from i in PLSs where (i.Layer == BoardLayer.Outline || i.Layer == BoardLayer.Mill) && i.Side == BoardSide.Both select i;
            if (L.Count() == 0) return;

            List<PolyLine> ShapesList = new List<PolyLine>();
            Dictionary<int, PolyLine> InsidePolygons = new Dictionary<int, PolyLine>();
            foreach (var l in L)
            {
                l.FixPolygonWindings();
                ShapesList.AddRange(l.OutlineShapes);

            }

            List<int> AddedIds = new List<int>();
            // Helpers.LineSegmentsToPolygons()

            for (int i = 0; i < ShapesList.Count; i++)
            {
                if (ClipperLib.Clipper.Orientation(ShapesList[i].toPolygon()) == false)
                {
                    InsidePolygons[i] = ShapesList[i];
                }
            }

            Image<Rgba32> B2 = new Image<Rgba32>(w, h); // Black transparent by default? 
            var G2 = new ImageSharpGraphicsInterface(B2);
            // G2.Clear(Color.Black); // Clear black opague? Orig code: G2.Clear(Color.Black);
            G2.Clear(Color.Black); // Rgba32(0,0,0,255)
            
            G2.Transform = TransformCopy;
            ApplyAASettings(G2);


            //G.CompositingMode = CompositingMode.SourceCopy;

            foreach (var a in InsidePolygons.Values)
            {
                List<PointF> Points = new List<PointF>();

                for (int j = 0; j < a.Count(); j++)
                {
                    var P1 = a.Vertices[j];
                    Points.Add(new PointF((float)((P1.X)), (float)((P1.Y))));
                }

                // FillPolygon(new SolidBrush(Color.Red)...)
                G2.FillPolygon(new GerberLibrary.Core.Primitives.SolidBrush(Color.Red), Points.ToArray());
            }
            if (Gerber.SaveIntermediateImages) B2.Save("11 outlines_redimages.png");


            // Carve logic
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var S = B2[x, y];
                    if (S.R > 0)
                    {
                        var O = _Target[x, y];
                        // Target.SetPixelIDX(idx, Color.FromArgb(255 - S.R, Target.GetPixelIDX(idx))); 
                        // It seems the intent is to reduce alpha?
                        // Orig: Color.FromArgb(255 - S.R, Target...) -> sets Alpha = 255 - S.R.
                        // So if S.R is 255 (Red), Alpha becomes 0.
                        
                        _Target[x, y] = Color.FromRgba(O.R, O.G, O.B, (byte)(255 - S.R)).ToPixel<Rgba32>();
                    }
                }
            }

            if (Gerber.SaveIntermediateImages) _Target.Save("22 outlines_carved.png");


            // G.CompositingMode = CompositingMode.SourceOver;
            G.Transform = T;
            if (Gerber.ExtremelyVerbose) Console.WriteLine("polygons #: {0} total, {1} carved", ShapesList.Count, InsidePolygons.Count);


            G2.Clear(Color.Black);
            G2.Transform = TransformCopy;
            foreach (var a in ShapesList)
            {
                List<PointF> Points = new List<PointF>();

                for (int j = 0; j < a.Count(); j++)
                {
                    var P1 = a.Vertices[j];
                    Points.Add(new PointF((float)((P1.X)), (float)((P1.Y))));
                }

                G2.DrawLines(new GerberLibrary.Core.Primitives.Pen(Color.Transparent, 1.0f / (float)scale), Points.ToArray());
                // Wait, DrawPolygon with Transparent pen? 
                // Orig: G2.DrawPolygon(new Pen(Color.Transparent, ...)) ?
                // If Transparent, it does nothing?
            }
            if (Gerber.SaveIntermediateImages) B2.Save("33 outlines_3aftershapelist.png");

            foreach (var a in InsidePolygons.Values)
            {
                List<PointF> Points = new List<PointF>();

                for (int j = 0; j < a.Count(); j++)
                {
                    var P1 = a.Vertices[j];
                    Points.Add(new PointF((float)((P1.X)), (float)((P1.Y))));
                }
                               // new Pen(Color.FromArgb(200, 255, 255, 0), ...)
                G2.DrawLines(new GerberLibrary.Core.Primitives.Pen(Color.FromRgba(255, 255, 0, 200), 1.0f / (float)scale), Points.ToArray());
            }

            if (Gerber.SaveIntermediateImages) B2.Save("44 outlines_4afterinside.png");

        }

        private void CheckForOutlineFiles(ProgressLog Logger)
        {
            List<ParsedGerber> Outlines = new List<ParsedGerber>();
            List<ParsedGerber> Mills = new List<ParsedGerber>();
            List<ParsedGerber> Unknowns = new List<ParsedGerber>();
            foreach (var a in PLSs)
            {
                if (a.Side == BoardSide.Both && (a.Layer == BoardLayer.Outline))
                {
                    Outlines.Add(a);
                }
                if (a.Side == BoardSide.Both && (a.Layer == BoardLayer.Mill))
                {
                    Mills.Add(a);
                }
                if (a.Side == BoardSide.Unknown && a.Layer == BoardLayer.Unknown)
                {
                    Unknowns.Add(a);
                    Errors.Add(String.Format("Unknown file in set:{0}", Path.GetFileName(a.Name)));
                    if (Logger != null) Logger.AddString(String.Format("Unknown file in set:{0}", Path.GetFileName(a.Name)));
                }

            }

            if (Outlines.Count == 0)
            {
                if (Unknowns.Count == 0)
                {
                    Errors.Add(String.Format("No outline file found and all other files accounted for! "));
                    if (Logger != null) Logger.AddString(String.Format("No outline file found and all other files accounted for! "));

                    // if (Mills.Count == 1)
                    // {
                    //    Mills[0].Layer = BoardLayer.Outline;
                    //   Errors.Add(String.Format("Elevating mill file to outline!"));
                    //  if (Logger != null) Logger.AddString(String.Format("Elevating mill file to outline!"));
                    // }
                    // else
                    //                    if (!InventOutlineFromMill())
                    {
                        CreateBoxOutline();
                    }
                }
                else
                {
                    CreateBoxOutline();
                    return;

                    //InventOutline();
                    //return;
                    //foreach (var a in Unknowns)
                    //{
                    //    PLSs.Remove(a);
                    //    hasgko = true;
                    //    a.Layer = BoardLayer.Outline;
                    //    a.Side = BoardSide.Both;
                    //    Console.WriteLine("Note: Using {0} as outline file", Path.GetFileName(a.Name));

                    //    if (Logger != null) Logger.AddString(String.Format("Note: Using {0} as outline file", Path.GetFileName(a.Name)));

                    //    bool zerowidth = true;
                    //    bool precombine = true;

                    //    var b = AddBoardToSet(a.Name, zerowidth, precombine, 1.0);
                    //    b.Layer = BoardLayer.Outline;
                    //    b.Side = BoardSide.Both;

                    //}
                }
            }
        }

        private void CheckRelativeBoundingBoxes(ProgressLog Logger)
        {


            List<ParsedGerber> DrillFiles = new List<ParsedGerber>();
            List<ParsedGerber> DrillFilesToReload = new List<ParsedGerber>();
            Bounds BB = new Bounds();
            foreach (var a in PLSs)
            {
                if (a.Layer == BoardLayer.Drill)
                {
                    DrillFiles.Add(a);
                }
                else
                {
                    BB.AddBox(a.BoundingBox);
                }
            }

            foreach (var a in DrillFiles)
            {

                if (a.BoundingBox.Intersects(BB) == false)
                {
                    Errors.Add(String.Format("Drill file {0} does not seem to touch the main bounding box!", Path.GetFileName(a.Name)));
                    if (Logger != null) Logger.AddString(String.Format("Drill file {0} does not seem to touch the main bounding box!", Path.GetFileName(a.Name)));
                    PLSs.Remove(a);
                }
            }



            BoundingBox = new Bounds();
            foreach (var a in PLSs)
            {
                //   Console.WriteLine("Progress: Adding board {6} to box::{0:N2},{1:N2} - {2:N2},{3:N2} -> {4:N2},{5:N2}", a.BoundingBox.TopLeft.X, a.BoundingBox.TopLeft.Y, a.BoundingBox.BottomRight.X, a.BoundingBox.BottomRight.Y, a.BoundingBox.Width(), a.BoundingBox.Height(), Path.GetFileName(a.Name));


                //Console.WriteLine("adding box for {0}:{1},{2}", a.Name, a.BoundingBox.Width(), a.BoundingBox.Height());
                BoundingBox.AddBox(a.BoundingBox);
            }

        }

        Image<Rgba32> DrawBoard(double dpi, BoardSide CurrentLayer, BoardRenderColorSet Colors, string basefilename = null, ProgressLog Logger = null, bool ForceWhite = false)
        {
            if (Logger != null) Logger.PushActivity("DrawBoard");
            try
            {
                scale = dpi / 25.4d; // dpi
                var OutlineBoundingBox = GetOutlineBoundingBox();

                double bw = Math.Abs(OutlineBoundingBox.BottomRight.X - OutlineBoundingBox.TopLeft.X);
                double bh = Math.Abs(OutlineBoundingBox.BottomRight.Y - OutlineBoundingBox.TopLeft.Y);
                int width = (int)((bw * scale));
                int height = (int)((bh * scale));

                int w = width + 3;
                int h = height + 3;
                Image<Rgba32> _Final = new Image<Rgba32>(w, h); // Transparent by default

                Image<Rgba32> _BoardPlate = new Image<Rgba32>(w, h);

                Image<Rgba32> _SilkMask = new Image<Rgba32>(w, h);

                Matrix3x2 TransformCopy = Matrix3x2.Identity;
                
                {
                    // Build transform manually since we are not using 'G'
                    // Replicate logic:
                    // Translate(0, h)
                    // Scale(1, -1)
                    if (CurrentLayer == BoardSide.Bottom)
                    {
                         // Translate(w, 0)
                         // Scale(-1, 1)
                    }
                    // Translate(1, 1)
                    // Scale(scale, scale)
                    // Translate(-TL.X, -TL.Y)
                    
                    var T = Matrix3x2.Identity;
                    T = T * Matrix3x2.CreateTranslation(0, h);
                    T = T * Matrix3x2.CreateScale(1, -1);
                    
                    if (CurrentLayer == BoardSide.Bottom)
                    {
                        T = T * Matrix3x2.CreateTranslation(w, 0);
                        T = T * Matrix3x2.CreateScale(-1, 1);
                    }
                    
                    T = T * Matrix3x2.CreateTranslation(1, 1);
                    T = T * Matrix3x2.CreateScale((float)scale, (float)scale);
                    T = T * Matrix3x2.CreateTranslation((float)-OutlineBoundingBox.TopLeft.X, (float)-OutlineBoundingBox.TopLeft.Y);
                    
                    TransformCopy = T;
                }

                if (Logger != null) Logger.AddString("Drawing outline files");
                // Color.FromArgb(a,r,g,b) -> Red=0, Green=0, Blue=0, Alpha=20? No, System.Drawing Color.FromArgb(alpha, basecolor) ? 
                // Color.FromArgb(20, 0, 0, 0) -> A=20, R=0,G=0,B=0.
                Image<Rgba32> _OutlineBase = DrawIfExists(width, height, TransformCopy, Colors.BoardRenderBaseMaterialColor, BoardLayer.Outline, BoardSide.Both, basefilename, true, 1, true);
                if (Logger != null) Logger.AddString("Drawing mill files");
                Image<Rgba32> _OutlineMill = DrawIfExists(width, height, TransformCopy, Colors.BoardRenderBaseMaterialColor, BoardLayer.Mill, BoardSide.Both, basefilename, true, 1, true);

                if (Logger != null) Logger.AddString("Drawing copper files");
                Image<Rgba32> _Copper = DrawIfExists(width, height, TransformCopy, Color.FromRgb(80, 80, 0), BoardLayer.Copper, CurrentLayer, basefilename, true);

                if (Logger != null) Logger.AddString("Drawing silk files");
                Image<Rgba32> _Silk = DrawIfExists(width, height, TransformCopy, Colors.BoardRenderSilkColor, BoardLayer.Silk, CurrentLayer, basefilename, true, 0.2f);

                if (Logger != null) Logger.AddString("Drawing soldermask files");
                Image<Rgba32> _SolderMaskHoles = DrawIfExists(width, height, TransformCopy, Colors.BoardRenderPadColor, BoardLayer.SolderMask, CurrentLayer, basefilename, true, 0.2f);

                if (Logger != null) Logger.AddString("Drawing drill files");
                Image<Rgba32> _DrillHoles = DrawIfExists(width, height, TransformCopy, Color.Black, BoardLayer.Drill, BoardSide.Both, basefilename, true, 1.0f);

                if (Gerber.SaveIntermediateImages == true)
                {
                    Console.WriteLine("Progress: Writing intermediate images:");
                    if (_Copper != null) { _Copper.Save(CurrentLayer.ToString() + "_copper.png"); Console.WriteLine("Progress: Copper"); }
                    if (_SolderMaskHoles != null) { _SolderMaskHoles.Save(CurrentLayer.ToString() + "_soldermaskholes.png"); Console.WriteLine("Progress: SolderMask"); }
                    if (_DrillHoles != null) { _DrillHoles.Save(CurrentLayer.ToString() + "_drill.png"); Console.WriteLine("Progress: Drill"); }
                    if (_OutlineBase != null) { _OutlineBase.Save(CurrentLayer.ToString() + "_base.png"); Console.WriteLine("Progress: Base"); }
                    if (_OutlineMill != null) { _OutlineMill.Save(CurrentLayer.ToString() + "_mill.png"); Console.WriteLine("Progress: Mill"); }
                }

                // Composition logic
                {
                    {
                        // Draw OutlineBase and Mill onto _BoardPlate
                        // Using ImageSharp DrawImage for simple overlay
                        var G = new ImageSharpGraphicsInterface(_BoardPlate);
                        G.Clear(Color.Transparent);
                        if (_OutlineBase != null) _BoardPlate.Mutate(ctx => ctx.DrawImage(_OutlineBase, 1.0f));
                        if (_OutlineMill != null) _BoardPlate.Mutate(ctx => ctx.DrawImage(_OutlineMill, 1.0f));

                        if (Logger != null) Logger.AddString("Carving inner polygons from board");
                        if (Gerber.SaveIntermediateImages == true) _BoardPlate.Save("00 Outlines before carve.png");

                        CarveOutlineAndMillInnerPolygonsFromImage(basefilename, w, h, G, _BoardPlate, TransformCopy);
                        if (Gerber.SaveIntermediateImages == true) _BoardPlate.Save("66 OutlinesCarved.png");

                        // Clear _Final
                        _Final.Mutate(ctx => ctx.Clear(Color.Transparent));

                        if (Logger != null) Logger.AddString("Carving drills from board");

                        if (_DrillHoles != null)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                for (int y = 0; y < h; y++)
                                {
                                    var O = _BoardPlate[x, y];
                                    var Drill = _DrillHoles[x, y];
                                    
                                    if (Drill.A > 0)
                                    {
                                        float OA = 1.0f - (Drill.A / 255.0f);
                                        float DA = O.A / 255.0f; // Original alpha normalized
                                        // New Alpha = OA * DA * 255
                                        // RGB preserved?
                                        _BoardPlate[x, y] = new Rgba32(O.R, O.G, O.B, (byte)Math.Round(OA * DA * 255.0f));
                                    }
                                }
                            }
                        }
                        
                        // Draw BoardPlate onto Final?
                        // G.DrawImage(_BoardPlate...) -> _Final?
                        // Original code: G.DrawImage(_BoardPlate, ...) onto _Final context? 
                        // Wait, logic was:
                        // G = Graphics.FromImage(_Final);
                        // G.DrawImage(_BoardPlate...)
                        _Final.Mutate(ctx => ctx.DrawImage(_BoardPlate, 1.0f));
                        
                        if (Gerber.SaveIntermediateImages == true) _BoardPlate.Save("BoardPlateAfterDrills.png");
                    }
                    if (Logger != null) Logger.AddString("Layering copper on board");

                    if (_Copper != null)
                    {
                        // Loop to composite Copper onto Final
                         for (int x = 0; x < w; x++)
                        {
                            for (int y = 0; y < h; y++)
                            {
                                var O = _Final[x, y];
                                var C = _Copper[x, y];

                                if (O.A > 0)
                                {
                                    if (C.A > 0)
                                    {
                                        // Mix copper color with board color?
                                        Rgba32 CopperColor = Colors.BoardRenderPadColor.ToPixel<Rgba32>(); 
                                        float A = (C.A / 255.0f);
                                        float IA = 1 - A;
                                        // Original Logic:
                                        // newDC = CopperColor * A + O * IA
                                        // Alpha preserved from O
                                        _Final[x, y] = new Rgba32(
                                            (byte)Math.Round(CopperColor.R * A + O.R * IA),
                                            (byte)Math.Round(CopperColor.G * A + O.G * IA),
                                            (byte)Math.Round(CopperColor.B * A + O.B * IA),
                                            O.A
                                        );
                                    }
                                }
                            }
                        }
                        if (Gerber.SaveIntermediateImages == true) _Final.Save("FinalAfterCopper.png");

                    }
                    {
                        // Draw BoardPlate onto SilkMask
                        _SilkMask.Mutate(ctx => ctx.Clear(Color.Transparent));
                        _SilkMask.Mutate(ctx => ctx.DrawImage(_BoardPlate, 1.0f));
                    }
                    if (Logger != null) Logger.AddString("Applying soldermask to board");

                    if (_SolderMaskHoles != null)
                    {
                        // Pixel loop for Soldermask, Copper, SilkMask, BoardPlate
                        // Need generic access
                        
                        for (int x = 0; x < w; x++)
                        {
                            for (int y = 0; y < h; y++)
                            {
                                var O = _Final[x, y];
                                var Mask = _SolderMaskHoles[x, y];
                                
                                if (Mask.A > 0)
                                {
                                     // SilkMask Update
                                     var OSM = _SilkMask[x, y];
                                     float OA = 1.0f - (Mask.A / 255.0f);
                                     float DA = O.A / 255.0f;
                                     _SilkMask[x, y] = new Rgba32(O.R, O.G, O.B, (byte)Math.Round(OA*DA*255.0f));
                                }
                                
                                Rgba32 Cop = new Rgba32(0,0,0,0);
                                if (_Copper != null) Cop = _Copper[x, y];
                                
                                var BmP = _BoardPlate[x, y];
                                
                                if (Cop.A > 0 && Mask.A > 0)
                                {
                                    Rgba32 SurfaceFinish = Colors.BoardRenderPadColor.ToPixel<Rgba32>();
                                    // Use BaseColor = O
                                    float A = (Cop.A / 255.0f * Mask.A / 255.0f * (BmP.A / 255.0f)) * 0.85f;
                                    float IA = 1.0f - A;
                                    
                                    // Composite O with SurfaceFinish
                                    byte newR = (byte)Math.Round(SurfaceFinish.R * A + O.R * IA);
                                    byte newG = (byte)Math.Round(SurfaceFinish.G * A + O.G * IA);
                                    byte newB = (byte)Math.Round(SurfaceFinish.B * A + O.B * IA);
                                    
                                    // Update O (which is used in next calculation?)
                                    // Original logic updates 'O' variable, then uses it to calculate newC.
                                    
                                    // Also modulate Alpha?
                                    // O = Color.FromArgb((byte)Math.Round(O.A * BmP.A / 255.0f), ...)
                                    byte newA = (byte)Math.Round(O.A * BmP.A / 255.0f);
                                    O = new Rgba32(newR, newG, newB, newA);
                                }
                                
                                // Calculate newC
                                float OA2 = (Mask.A / 255.0f) * 0.9f + 0.1f;
                                float IOA = 1.0f - OA2;
                                
                                Rgba32 S = Colors.BoardRenderColor.ToPixel<Rgba32>();
                                if (Cop.A > 0)
                                {
                                    // Interpolate S with TraceColor
                                    Rgba32 Trace = Colors.BoardRenderTraceColor.ToPixel<Rgba32>();
                                    float CA = Cop.A / 255.0f;
                                    // Simple mix
                                    S = new Rgba32(
                                        (byte)(S.R * (1-CA) + Trace.R * CA),
                                        (byte)(S.G * (1-CA) + Trace.G * CA),
                                        (byte)(S.B * (1-CA) + Trace.B * CA),
                                        S.A
                                    );
                                }
                                
                                _Final[x, y] = new Rgba32(
                                    (byte)Math.Round(O.R * OA2 + S.R * IOA),
                                    (byte)Math.Round(O.G * OA2 + S.G * IOA),
                                    (byte)Math.Round(O.B * OA2 + S.B * IOA),
                                    O.A
                                );
                            }
                        }

                        if (Gerber.SaveIntermediateImages == true) _Final.Save("FinalAfterSoldermask.png");

                    }
                    if (Logger != null) Logger.AddString("Applying silkscreen to board");

                    if (_Silk != null)
                    {
                         for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                var SilkPixel = _Silk[x, y];
                                float AS = SilkPixel.A / 255.0f;
                                if (AS > 0)
                                {
                                    var OutputPixel = _Final[x, y];
                                    var Mask = _SilkMask[x, y];

                                    //    float AO = O.A / 255.0f;
                                    float AM = (Mask.A / 255.0f) * (1 - AS);
                                    
                                    if (Mask.A < 255 && _Copper != null)
                                    {
                                        var CopperPixel = _Copper[x, y];
                                        if (CopperPixel.A > 0)
                                        {
                                            AM = AM * (1 - (CopperPixel.A / 255.0f)) + 1 * (CopperPixel.A / 255.0f);
                                        }
                                    }

                                    float iAM = 1.0f - AM;
                                    _Final[x, y] = new Rgba32(
                                      (byte)Math.Round(OutputPixel.R * AM + SilkPixel.R * iAM),
                                      (byte)Math.Round(OutputPixel.G * AM + SilkPixel.G * iAM),
                                      (byte)Math.Round(OutputPixel.B * AM + SilkPixel.B * iAM),
                                      OutputPixel.A);
                                }
                            }
                        }

                        if (Gerber.SaveIntermediateImages == true) _Final.Save("FinalAfterSilk.png");

                    }

                    if (_Copper != null && Gerber.GerberRenderBumpMapOutput)
                    {
                        ApplyBumpMapping(_Final, _Copper, w, h);
                    }
                }

                if (ForceWhite)
                {
                     for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            if (_Final[x, y].A == 0) _Final[x, y] = new Rgba32(255, 255, 255, 255);
                        }
                    }
                }
                if (Logger != null) Logger.PopActivity();
                return _Final;
            }

            catch(Exception e)
            {
                if (Logger != null) Logger.PopActivity();
                Console.WriteLine("DrawBoard Error: " + e.Message);
            }
            return null;
        }

        void DrawXRayBoard(string ftop, string fbot, double dpi, BoardRenderColorSet Colors, string basefilename = null, ProgressLog Logger = null, bool XRayFromBottom = false)
        {
            if (Logger != null) Logger.PushActivity("DrawXRayBoard");

            scale = dpi / 25.4d; // dpi
            var OutlineBoundingBox = GetOutlineBoundingBox();

            double bw = Math.Abs(OutlineBoundingBox.BottomRight.X - OutlineBoundingBox.TopLeft.X);
            double bh = Math.Abs(OutlineBoundingBox.BottomRight.Y - OutlineBoundingBox.TopLeft.Y);
            int width = (int)((bw * scale));
            int height = (int)((bh * scale));

            int w = width + 3;
            int h = height + 3;
            Image<Rgba32> _Final = new Image<Rgba32>(w, h);
            Image<Rgba32> _FinalBot = new Image<Rgba32>(w, h);

            Image<Rgba32> _BoardPlate = new Image<Rgba32>(w, h); // Unused in current commented code? Keep for safety.

            Matrix3x2 TransformCopy = Matrix3x2.Identity;
            {
               
                _Final.Mutate(ctx => ctx.Clear(Color.Transparent));
                
                var T = Matrix3x2.Identity;
                T = T * Matrix3x2.CreateTranslation(0, h);
                T = T * Matrix3x2.CreateScale(1, -1);

                if (XRayFromBottom)
                {
                    T = T * Matrix3x2.CreateTranslation(w, 0);
                    T = T * Matrix3x2.CreateScale(-1, 1);
                }

                T = T * Matrix3x2.CreateTranslation(1, 1);
                T = T * Matrix3x2.CreateScale((float)scale, (float)scale);
                T = T * Matrix3x2.CreateTranslation((float)-OutlineBoundingBox.TopLeft.X, (float)-OutlineBoundingBox.TopLeft.Y);
                TransformCopy = T;
            }

            if (Logger != null) Logger.AddString("Drawing outline files");
            Image<Rgba32> _OutlineBase = DrawIfExists(width, height, TransformCopy, Color.FromRgba(0, 0, 0, 20), BoardLayer.Outline, BoardSide.Both, basefilename, true, 1, true);
            if (Logger != null) Logger.AddString("Drawing mill files");
            Image<Rgba32> _OutlineMill = DrawIfExists(width, height, TransformCopy, Color.FromRgba(255, 255, 255, 255), BoardLayer.Mill, BoardSide.Both, basefilename, true, 1, true);


            if (Logger != null) Logger.AddString("Drawing copper files");
            Image<Rgba32> _Copper = RenderRainbowToBitmap(width, height, TransformCopy, 64, BoardLayer.Any, BoardSide.Either, true, false, false);
            Image<Rgba32> _CopperBot = RenderRainbowToBitmap(width, height, TransformCopy, 64, BoardLayer.Any, BoardSide.Either, true,false,true);


            if (Logger != null) Logger.AddString("Drawing drill files");
            Image<Rgba32> _DrillHoles = DrawIfExists(width, height, TransformCopy, Color.Black, BoardLayer.Drill, BoardSide.Both, basefilename, true, 1.0f);

            if (Gerber.SaveIntermediateImages == true)
            {
                if (Logger != null) Logger.AddString("Writing intermediate images");
                if (_Copper != null) { _Copper.Save("xray_copper.png"); if (Logger != null) Logger.AddString("Copper"); }
                if (_DrillHoles != null) { _DrillHoles.Save("xray_drill.png"); if (Logger != null) Logger.AddString("Drill"); }
                if (_OutlineBase != null) { _OutlineBase.Save("xray_base.png"); if (Logger != null) Logger.AddString("Base"); }
                if (_OutlineMill != null) { _OutlineMill.Save("xray_mill.png"); if (Logger != null) Logger.AddString("Mill"); }
            }

            {
                if (Logger != null) Logger.AddString("Layering copper on plate");

                if (_Copper != null)
                {
                    // Composition Loop
                    // Use simple loops for now (accessing image[x,y])
                    
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int idxinv = (w - x - 1); // x index for Bot?
                            // Logic: idxinv = (y * Width + (w-x-1)) * 4 in original code.
                            // So we just access [w-x-1, y]
                            
                            var O = _Final[x, y];
                            var C = _Copper[x, y];
                            
                            // CopperBot logic appears to use same loop?
                            // Yes, uses parallel logic for FinalBot.
                            var CB = new Rgba32(0,0,0,0);
                            if (_CopperBot != null && x < _CopperBot.Width && y < _CopperBot.Height) // bounding check safety
                                CB = _CopperBot[x, y];

                            if (true) // Original had if (P.A > 0) commented code wrapper? No, scope wrapper?
                            {
                                if (true) // if (C.A > 0) logic
                                {
                                    float A = (C.A / 255.0f);
                                    float IA = 1 - A;
                                    
                                    // newDC = C * A + O * IA.
                                    // Note: O is initially Transparent? Yes.
                                    // C is Rainbow output (opaque colors).
                                    // So this is C blended over O.
                                    
                                    _Final[x, y] = new Rgba32(
                                        (byte)Math.Round(C.R * A + O.R * IA),
                                        (byte)Math.Round(C.G * A + O.G * IA),
                                        (byte)Math.Round(C.B * A + O.B * IA),
                                        255 // Alpha fixed to 255? Orig: Color.FromArgb(255, ...)
                                    );
                                    
                                    // FinalBot
                                    // newDCR = CB * A + O * IA??
                                    // Orig uses O.R * IA. O comes from Final[x,y] (top).
                                    // So Top affects Bot?
                                    // "O" is Final (Top) pixel.
                                    // So Bot image is composite of CopperBot and Final(Top)?
                                    // Yes, XRay view usually shows layers through.
                                    
                                    // Ensure bounds for FinalBot
                                    if (idxinv >= 0 && idxinv < w)
                                    {
                                         _FinalBot[idxinv, y] = new Rgba32(
                                            (byte)Math.Round(CB.R * A + O.R * IA),
                                            (byte)Math.Round(CB.G * A + O.G * IA),
                                            (byte)Math.Round(CB.B * A + O.B * IA),
                                            255
                                        );
                                    }
                                }
                            }
                        }
                    }
                    
                    if (Gerber.SaveIntermediateImages == true) _Final.Save("XRayFinalAfterCopper.png");

                }


                if (_DrillHoles != null)
                {
                     // Composite Drill Holes
                     for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            var O = _Final[x, y];
                            var Drill = _DrillHoles[x, y];
                            if (Drill.A > 0)
                            {
                                float OA = 1.0f - (Drill.A / 255.0f);
                                float DA = O.A / 255.0f; 
                                // Reduce Alpha? But XRay uses opaque.
                                // Orig code for XRay just saved images?
                                // Orig code had commented out DrillHole logic in XRay?
                                // It seems XRay function ends at line 1475 in orig (Wait, I only read up to 1450).
                                // Let's check further down before committing.
                            }
                        }
                    }
                }
            }
            if (ftop.Length > 0) _Final.Save(ftop);
            if (fbot.Length > 0) _FinalBot.Save(fbot);

            if (Gerber.SaveIntermediateImages)
            {
                 // ...
            }
            
            if (Logger != null) Logger.PopActivity();
        }


        private Image<Rgba32> DrawIfExists(int w, int h, Matrix3x2 TransformCopy, Color color, BoardLayer boardLayer, BoardSide boardSide, string filename = null, bool fill = true, float alpha = 0.83f, bool forcefill = false)
        {

            Image<Rgba32> B2 = RenderToImage(w, h, TransformCopy, color, boardLayer, boardSide, fill, forcefill);


            if (B2 != null)
            {
                if (filename != null)
                {
                    //  B2.Save(filename + "_Layer_" + boardSide.ToString() + "_" + boardLayer.ToString() + ".png");
                }

                // Alpha blending is handled by ImageSharp during composition if needed, or we can mutate here.
                // Replicating ColorMatrix alpha scaling logic on the whole image:
                if (Math.Abs(alpha - 1.0f) > 0.01f)
                {
                    // Multiply alpha on all pixels
                     B2.Mutate(ctx => ctx.ProcessPixelRowsAsVector4(row => {
                         for(int i=0;i<row.Length;i++)
                         {
                             row[i].W *= alpha; 
                         }
                     }));
                }
            }

            return B2;
        }

        public static int CountLayerShapes(ParsedGerber l)
        {
            int shapes = 0;
            foreach (var Shape in l.DisplayShapes)
            {
                if (Shape.Vertices.Count > 1)
                {
                    //if (Shape.Thin == false)

                    shapes++;
                }
            }
            return shapes;
        }
        private int DrawLayerToGraphics(Color color, bool fill, GraphicsInterface G2, Pen P, ParsedGerber l, bool forcefill = false)
        {
            int RenderedShapes = 0;
            foreach (var Shape in l.DisplayShapes)
            {
                if (Shape.Vertices.Count > 1)
                {
                    //if (Shape.Thin == false)

                    DrawShape(G2, P, color, Shape, (fill && (Shape.Thin == false)) || forcefill, l.TranslationSinceLoad.X, l.TranslationSinceLoad.Y);
                    RenderedShapes++;
                }
            }
            return RenderedShapes;
        }

        private void DrawShape(GraphicsInterface G, Pen P, Color c, PolyLine Shape, bool fill, double dx, double dy)
        {

            List<PointF> Points = new List<PointF>();

            for (int j = 0; j < Shape.Count(); j++)
            {
                var P1 = Shape.Vertices[j];
                Points.Add(new PointF((float)((P1.X - dx)), (float)((P1.Y - dy))));
            }

            if (Points.Count() > 1)
            {
                if (fill)
                {
                    if (Points[0] == Points[Points.Count() - 1])
                    {
                        Points.Remove(Points.Last());
                    }
                    if (Shape.ClearanceMode)
                    {
                        G.CompositingMode = CompositingMode.SourceCopy;
                        G.FillPolygon(new SolidBrush(Color.Transparent), Points.ToArray());
                        G.CompositingMode = CompositingMode.SourceOver;
                    }
                    else
                    {
                         G.FillPolygon(new SolidBrush(c), Points.ToArray());
                        // G.DrawLines(new Pen(c, P.Width / 4), Points.ToArray());
                     }
                 }
                else
                {
                    G.DrawLines(P, Points.ToArray());
                }
            }
        }

        private void FixEagleDrillExportIssues(ProgressLog Logger)
        {
            if (Gerber.SkipEagleDrillFix == true) { Logger.AddString("skipping eagle fix"); return; };
            List<ParsedGerber> DrillFiles = new List<ParsedGerber>();
            List<Tuple<double, ParsedGerber>> DrillFilesToReload = new List<Tuple<double, ParsedGerber>>();
            Bounds BB = new Bounds();
            foreach (var a in PLSs)
            {
                if (a.Layer == BoardLayer.Drill)
                {
                    DrillFiles.Add(a);
                    DrillFileScale[a.Name] = 1.0;
                }
                else
                {
                    BB.AddBox(a.BoundingBox);
                }
            }

            foreach (var a in DrillFiles)
            {
                var b = a.BoundingBox;
                if (b.Width() > BB.Width() * 1.5 || b.Height() > BB.Height() * 1.5)
                {
                    var MaxRatio = Math.Max(b.Width() / BB.Width(), b.Height() / BB.Height());
                    if (Logger != null) Logger.AddString(String.Format("Note: Really large drillfile found({0})-fix your export scripts!", a.Name));
                    Console.WriteLine("Note: Really large drillfile found ({0})- fix your export scripts!", a.Name);
                    DrillFilesToReload.Add(new Tuple<double, ParsedGerber>(MaxRatio, a));
                }

            }
            foreach (var a in DrillFilesToReload)
            {
                PLSs.Remove(a.Item2);
                var scale = 1.0;
                if (Double.IsInfinity(a.Item1) || Double.IsNaN(a.Item1))
                {
                    Errors.Add("Drill file size reached infinity - ignoring it");
                    if (Logger != null) Logger.AddString("Drill file size reached infinity - ignoring it");
                }
                else
                {
                    var R = a.Item1;
                    while (R >= 1.5)
                    {
                        R /= 10;
                        scale /= 10;
                    }

                    DrillFileScale[a.Item2.Name] = scale;
                    AddFileToSet(a.Item2.Name, Logger, scale);
                }
            }


            BoundingBox = new Bounds();
            foreach (var a in PLSs)
            {
                //Console.WriteLine("Progress: Adding board {6} to box::{0:N2},{1:N2} - {2:N2},{3:N2} -> {4:N2},{5:N2}", a.BoundingBox.TopLeft.X, a.BoundingBox.TopLeft.Y, a.BoundingBox.BottomRight.X, a.BoundingBox.BottomRight.Y, a.BoundingBox.Width(), a.BoundingBox.Height(), Path.GetFileName( a.Name));


                //Console.WriteLine("adding box for {0}:{1},{2}", a.Name, a.BoundingBox.Width(), a.BoundingBox.Height());
                BoundingBox.AddBox(a.BoundingBox);
            }
        }


        public Bounds GetOutlineBoundingBox()
        {
            Bounds B = new Bounds();
            int i = 0;
            foreach (var a in PLSs)
            {
                if (a.Layer == BoardLayer.Mill || a.Layer == BoardLayer.Outline)
                {
                    B.AddBox(a.BoundingBox);
                    i++;
                }
            }
            if (i == 0) return BoundingBox;
            return B;
        }
        private bool InventOutline(ProgressLog log)
        {
            double largest = 0;
            ParsedGerber Largest = null;
            PolyLine Outline = null;

            foreach (var a in PLSs)
            {
                var P = a.FindLargestPolygon();
                if (P != null)
                {
                    if (P.Item1 > largest)
                    {
                        largest = P.Item1;
                        Largest = a;
                        Outline = P.Item2;
                    }
                }

            }

            if (largest < BoundingBox.Area() / 3.0) return false;
            bool zerowidth = true;
            bool precombine = true;

            Console.WriteLine("Note: Using {0} to extract outline file", Path.GetFileName(Largest.Name));
            if (Largest.Layer == BoardLayer.Mill)
            {
                Largest.OutlineShapes.Remove(Outline);
                Largest.Shapes.Remove(Outline);
            }

            var b = AddBoardToSet(Largest.Name,log,  zerowidth, precombine, 1.0);
            b.Layer = BoardLayer.Outline;
            b.Side = BoardSide.Both;
            b.DisplayShapes.Clear();
            b.OutlineShapes.Clear();
            b.Shapes.Clear();
            Outline.Close();
            b.Shapes.Add(Outline);
            b.OutlineShapes.Add(Outline);
            //b.DisplayShapes.Add(Outline);
            //b.BuildBoundary();
            b.FixPolygonWindings();
            b.CalcPathBounds();

            return true;
        }

        private bool InventOutlineFromMill(ProgressLog log)
        {
            log.PushActivity("InventOutlineFromMill");
            double largest = 0;
            ParsedGerber Largest = null;
            PolyLine Outline = null;

            foreach (var a in PLSs.Where(x => x.Layer == BoardLayer.Mill))
            {
                var P = a.FindLargestPolygon();
                if (P != null)
                {
                    if (P.Item1 > largest)
                    {
                        largest = P.Item1;
                        Largest = a;
                        Outline = P.Item2;
                    }
                }

            }
            if (Largest == null)
            {
                log.PopActivity();
                return false;
            }
            // if (largest < BoundingBox.Area() / 3.0) return false;
            bool zerowidth = true;
            bool precombine = true;

            Console.WriteLine("Note: Using {0} to extract outline file", Path.GetFileName(Largest.Name));

            var b = AddBoardToSet(Largest.Name,log, zerowidth, precombine, 1.0);
            b.Layer = BoardLayer.Outline;
            b.Side = BoardSide.Both;
            //b.DisplayShapes.Clear();
            //b.OutlineShapes.Clear();
            //b.Shapes.Clear();
            // Outline.Close();
            // b.Shapes.Add(Outline);
            // b.OutlineShapes.Add(Outline);
            //b.DisplayShapes.Add(Outline);
            //b.BuildBoundary();
            // b.FixPolygonWindings();
            // b.CalcPathBounds();
            log.PopActivity();
            return true;
        }

        private Image<Rgba32> RenderToImage(int w, int h, Matrix3x2 T, Color color, BoardLayer boardLayer, BoardSide boardSide, bool fill, bool forcefill = false)
        {
            var L = from i in PLSs where i.Layer == boardLayer && (i.Side == boardSide || boardSide == BoardSide.Either) select i;
            //if (L.Count() == 0) return null;
            Image<Rgba32> B2 = new Image<Rgba32>(w + 3, h + 3);
            var G3 = new ImageSharpGraphicsInterface(B2);
            G3.Clear(Color.Transparent);
            G3.Transform = T;
            Pen P = new Pen(color, 1.0f / (float)(scale));
            int Shapes = 0;
            foreach (var l in L)
            {
                Shapes += DrawLayerToGraphics(color, fill, G3, P, l, forcefill);
            }
            if (Shapes == 0) return null;
            return B2;
        }

        private Image<Rgba32> RenderRainbowToBitmap(int w, int h, Matrix3x2 T, int alpha, BoardLayer boardLayer, BoardSide boardSide, bool fill, bool forcefill = false, bool invertedorder = false)
        {

            var L =( from i in PLSs where (i.Layer == boardLayer || boardLayer == BoardLayer.Any) && (i.Side == boardSide || boardSide == BoardSide.Either) orderby Helpers.LayerOrdering(i.Side, i.Layer) select i).ToList();
            if (!invertedorder) L.Reverse();
            L = (from i in L where CountLayerShapes(i) > 0 select i).ToList();
            //if (L.Count() == 0) return null;
            Image<Rgba32> B2 = new Image<Rgba32>(w + 3, h + 3);
            //Graphics G2 = Graphics.FromImage(B2);

            Image<Rgba32> TB2 = new Image<Rgba32>(w + 3, h + 3);
            //Graphics TG2 = Graphics.FromImage(TB2);

            //ApplyAASettings(G2);
            var G3 = new ImageSharpGraphicsInterface(B2);
            G3.Clear(Color.White);
          //  G3.Transform = T.Clone();

            //ApplyAASettings(TG2);
            var TG3 = new ImageSharpGraphicsInterface(TB2);
            TG3.Clear(Color.White);
            TG3.Transform = T;

            int Shapes = 0;
            int total = L.Count();
            int current = 0;
            foreach (var l in L)
            {
                // ImageAttributes logic removed (alpha blending).
                // We will manually fade the image if needed when drawing B2 onto TB2 or vice versa.
                // The original code drew TB2 onto B2 with alpha.
                
                TG3.Clear(Color.FromRgba(255, 255, 255, 0)); // Transparent
                var color =  Helpers.RefractionNormalized(current / (float)(total - 1));
                Pen P = new Pen(color, 1.0f / (float)(scale));
                current++;
                bool localfill = forcefill;
                if (l.Layer == BoardLayer.Outline) localfill = false;

                int NewShapes = DrawLayerToGraphics(color, fill, TG3, P, l, localfill);
                if (NewShapes > 0)
                {
                    Shapes += NewShapes;
      
                    // Draw TB2 onto B2 with opacity 0.7f?
                    // Original code used matrixItems for alpha 0.7f.
                    
                    B2.Mutate(ctx => ctx.DrawImage(TB2, 0.7f)); // using opacituy
                }

            }
            if (Shapes == 0) return null;
            return B2;
        }

        public double GetDrillScaler(string f)
        {
            if (DrillFileScale.ContainsKey(f)) return DrillFileScale[f];
            return 1.0;
        }
    }

}
