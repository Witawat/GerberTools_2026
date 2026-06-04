using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TilingLibrary.Compatibility;
using Bitmap = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using Graphics = TilingLibrary.Compatibility.GraphicsInterface;
using Color = SixLabors.ImageSharp.Color;
using Font = SixLabors.Fonts.Font;
using RectangleF = SixLabors.ImageSharp.RectangleF;
using SolidBrush = TilingLibrary.Compatibility.Primitives.SolidBrush;
using PointF = SixLabors.ImageSharp.PointF;
using Primitives = TilingLibrary.Compatibility.Primitives;

namespace Artwork
{
    public static class Announcer
    {
        public static void DrawAnnouncement(Bitmap B, AnnouncementDetails AD)
        {
            Graphics G = new ImageSharpGraphicsInterface(B);
            Bitmap B2 = new Bitmap(B.Width, B.Height);
            Graphics G2 = new ImageSharpGraphicsInterface(B2);

            G2.InterpolationMode = Primitives.GraphicsInterpolationMode.High;
            G2.SmoothingMode = Primitives.SmoothingMode.AntiAlias;
            G.InterpolationMode = Primitives.GraphicsInterpolationMode.High;
            G.SmoothingMode = Primitives.SmoothingMode.AntiAlias;
            G.TextRenderingHint = Primitives.TextRenderingHint.AntiAlias;
            G2.TextRenderingHint = Primitives.TextRenderingHint.AntiAlias;


            G.Clear(Color.Black);

            if (AD.Background != null)
            {


                RectangleF From = new RectangleF(0, 0, AD.Background.Width, AD.Background.Height);
                RectangleF To = new RectangleF(0, 0, B.Width, B.Height);

                if (From.Width > To.Width || From.Height > To.Height)
                {
                    double fromas = From.Width / From.Height;
                    double toas = To.Width / To.Height;
                    double scale = 1;
                    if (fromas < toas)
                    {
                        scale = From.Width / To.Width;
                        From.Y = From.Height / 2 - (float)(To.Height * scale) / 2;

                        From.Height = (float)(To.Height * scale);

                    }
                    else
                    {
                        scale = From.Height / To.Height;
                        From.X = From.Width / 2 - (float)(To.Width * scale) / 2;
                           From.Width = (float)(To.Width * scale);
                        
                    }



                    Console.WriteLine("{0}", scale);
                }
                // ImageSharp doesn't have GraphicsUnit, so we'll use the simpler DrawImage
                var resized = AD.Background.Clone(ctx => ctx.Resize((int)To.Width, (int)To.Height));
                G.DrawImage(resized, To.X, To.Y, To.Width, To.Height);
            }

            for (int i = 0; i < 5; i++)
            {
                DrawTo(AD, G2, 10 + i *5, true);
            }

             TINRSArtWorkRenderer ArtRender = new TINRSArtWorkRenderer();
            Settings TheSettings = new Settings();
            TheSettings.InvertSource = false;
            TheSettings.DegreesOff = 14;
            //TheSettings.MaxSubDiv;

           
            ArtRender.BuildTree(B2, TheSettings);
            ArtRender.BuildStuff(B2, TheSettings);
            ArtRender.DrawTiling(TheSettings, B2, G, Color.Yellow, Color.Black, 1.2f, false);
            DrawTo(AD, G, 2, false);



        }

        private static void DrawTo(AnnouncementDetails AD, Graphics G, double fuzz, bool mask)
        {
            DrawTitle(AD, AD.Title, G, fuzz,10,10,mask, PantonBig);
            DrawTitle(AD,AD.Bodytext, G, fuzz, 20, 120, mask, Panton);
            DrawTitle(AD, "www.thisisnotrocketscience.nl", G, fuzz, 0, AD.Height-Panton.Size, mask, Panton, true);


        }
        public static Font Panton = SystemFonts.CreateFont("Panton Bold", 20);
        public static Font PantonBig = SystemFonts.CreateFont("Panton Bold", 30);

        private static void DrawTitle(AnnouncementDetails AD, string lbl, Graphics g, double fuzz, double x, double y, bool mask, Font F, bool rightalign = false)
        {
            SolidBrush B = new SolidBrush(Color.Black);

            if (mask)
            {
                B = new SolidBrush(Color.White);

            }

            if (rightalign)
            {
                var S = g.MeasureString(lbl);
                x = AD.Width - S.X - x;
            }
            for (double i =0;i<20;i++)
            {
                double p = Math.PI*2 *( i / 20.0);
                
                g.DrawString(new PointF((float)(x + Math.Cos(p)*fuzz), (float)(y + Math.Sin(p) * fuzz)), lbl, F.Size, false);
            }
           g.DrawString(new PointF((float)x, (float)y), lbl, F.Size, false);
        }
    }

    public class AnnouncementDetails
    {
        public string Title;
        public string Bodytext;
        public Bitmap Background;
        public DateTime ShowDate;
        public float Height = 640;
        public float Width = 640;
    }
}
