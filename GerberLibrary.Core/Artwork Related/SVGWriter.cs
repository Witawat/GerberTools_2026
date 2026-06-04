using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;
using System.Numerics;
using TriangleNet.Geometry;
using TriangleNet.Topology; // Guessing namespace for Triangle
using TriangleNet;
using Pen = GerberLibrary.Core.Primitives.Pen;
using SolidBrush = GerberLibrary.Core.Primitives.SolidBrush;

namespace GerberLibrary
{

    public class SVGGraphicsInterface : GerberLibrary.Core.GraphicsInterface
    {
        public double Width;
        public double Height; // Keep Height field as it's used in constructor and Save method

        public void FillRectangleCMYK(double x1, double y1, double width, double height, double c, double m, double y, double k)
        {
            OutputLines.Add(String.Format("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\"/>",D(x1),D(y1),D(width), D(height), GetCMYK(c,m,y,k)));
        }

        public void FillCircleCMYK(double x1, double y1, double radius, double c, double m, double y, double k)
        {
            OutputLines.Add(String.Format("<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" fill=\"{3}\"/>", D(x1), D(y1), D(radius), GetCMYK(c, m, y, k)));
        }

        public override void DrawString(PointF location, string text, double size, bool center = false)
        {
             OutputLines.Add(String.Format("<!-- Text: {0} at {1},{2} -->", text, location.X, location.Y));
        }

        public void AddCMYKText(string text, string fontfam, double x, double y1 , double height, double c, double m, double y, double k)
        {
            OutputLines.Add(String.Format("<text font-family=\"{0}\" font-size=\"{1}\" x=\"{2}\" y=\"{3}\" fill=\"{5}\">{4}</text>", fontfam, D(height), D(x), D(y1), text, GetCMYK(c,m,y,k)));
        }

        List<string> OutputLines = new List<string>();

        public override RectangleF ClipBounds => new RectangleF(0,0,(float)Width, (float)Height); // Changed Width to Height here
        public override bool IsFast => true;
        public override GraphicsInterpolationMode InterpolationMode { get; set; }
        public override Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;

        public override void Clear(Color color)
        {
            OutputLines.Add(String.Format("<rect x=\"0\" y=\"0\" width=\"{0}\" height=\"{1}\" fill=\"{2}\"/>", D(Width), D(Height), GetColor(color))); // Changed "100%" to D(Width) and D(Height)
        }

        public override void DrawImage(Image image, float x, float y, float w, float h)
        {
             // Placeholder
        }

        public override void DrawLine(GerberLibrary.Core.Primitives.Pen pen, PointF p1, PointF p2)
        {
              OutputLines.Add(String.Format("<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" />", D(p1.X), D(p1.Y), D(p2.X), D(p2.Y), GetColor(pen.Color), D(pen.Width)));
        }

        public override void DrawLines(GerberLibrary.Core.Primitives.Pen pen, PointF[] points)
        {
             if (points.Length < 2) return;
             string pts = "";
             foreach(var p in points) pts += $"{D(p.X)},{D(p.Y)} ";
             OutputLines.Add(String.Format("<polyline points=\"{0}\" fill=\"none\" stroke=\"{1}\" stroke-width=\"{2}\" />", pts, GetColor(pen.Color), D(pen.Width)));
        }

        public override void DrawPolygon(GerberLibrary.Core.Primitives.Pen pen, PointF[] points)
        {
             string pts = "";
             foreach(var p in points) pts += $"{D(p.X)},{D(p.Y)} ";
             OutputLines.Add(String.Format("<polygon points=\"{0}\" fill=\"none\" stroke=\"{1}\" stroke-width=\"{2}\" />", pts, GetColor(pen.Color), D(pen.Width)));
        }

        public override void FillPolygon(GerberLibrary.Core.Primitives.SolidBrush brush, PointF[] points)
        {
             string pts = "";
             foreach(var p in points) pts += $"{D(p.X)},{D(p.Y)} ";
             Color C = brush.Color;
             OutputLines.Add(String.Format("<polygon points=\"{0}\" fill=\"{1}\" stroke=\"none\" />", pts, GetColor(C)));
        }

        public override void FillRectangle(GerberLibrary.Core.Primitives.SolidBrush brush, float x, float y, float w, float h)
        {
             Color C = brush.Color;
             OutputLines.Add(String.Format("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" stroke=\"none\"/>", D(x), D(y), D(w), D(h), GetColor(C)));
        }
        
        public override void DrawRectangle(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h)
        {
             OutputLines.Add(String.Format("<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"none\" stroke=\"{4}\" stroke-width=\"{5}\"/>", D(x), D(y), D(w), D(h), GetColor(pen.Color), D(pen.Width)));
        }

        public override void DrawEllipse(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h)
        {
            OutputLines.Add(String.Format("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" fill=\"none\" stroke=\"{4}\" stroke-width=\"{5}\"/>", D(x+w/2), D(y+h/2), D(w/2), D(h/2), GetColor(pen.Color), D(pen.Width)));
        }

        public override void FillEllipse(GerberLibrary.Core.Primitives.SolidBrush brush, float x, float y, float w, float h)
        {
             Color C = brush.Color;
             OutputLines.Add(String.Format("<ellipse cx=\"{0}\" cy=\"{1}\" rx=\"{2}\" ry=\"{3}\" fill=\"{4}\" stroke=\"none\"/>", D(x+w/2), D(y+h/2), D(w/2), D(h/2), GetColor(C)));
        }

        public override void DrawPath(GerberLibrary.Core.Primitives.Pen pen, IPath path)
        {
            foreach (var simplePath in path.Flatten())
            {
                 var points = simplePath.Points.Span;
                 if (points.Length < 2) continue;
                 string d = $"M {D(points[0].X)} {D(points[0].Y)}";
                 for(int i=1; i<points.Length; i++)
                 {
                      d += $" L {D(points[i].X)} {D(points[i].Y)}";
                 }
                 if (simplePath.IsClosed) d += " Z";
                 OutputLines.Add(String.Format("<path d=\"{0}\" fill=\"none\" stroke=\"{1}\" stroke-width=\"{2}\" stroke-linejoin=\"round\" />", d, GetColor(pen.Color), D(pen.Width)));
            }
        }

        public override void FillPath(GerberLibrary.Core.Primitives.SolidBrush brush, IPath path)
        {
            Color C = brush.Color;

            foreach (var simplePath in path.Flatten())
            {
                 var points = simplePath.Points.Span;
                 if (points.Length < 2) continue;
                 string d = $"M {D(points[0].X)} {D(points[0].Y)}";
                 for(int i=1; i<points.Length; i++)
                 {
                      d += $" L {D(points[i].X)} {D(points[i].Y)}";
                 }
                 if (simplePath.IsClosed) d += " Z";
                 OutputLines.Add(String.Format("<path d=\"{0}\" fill=\"{1}\" stroke=\"none\" />", d, GetColor(C)));
            }
        }

        public override void DrawString(string text, SixLabors.Fonts.Font font, GerberLibrary.Core.Primitives.SolidBrush brush, PointF location, SixLabors.Fonts.TextOptions options)
        {
            float x = options.Origin.X;
            float y = options.Origin.Y;
            
            Color C = brush.Color;
            
            string anchor = "middle";
            if (options.HorizontalAlignment == HorizontalAlignment.Left) anchor = "start";
            if (options.HorizontalAlignment == HorizontalAlignment.Right) anchor = "end";
            
            OutputLines.Add(String.Format("<text x=\"{0}\" y=\"{1}\" font-family=\"{2}\" font-size=\"{3}\" fill=\"{4}\" text-anchor=\"{5}\">{6}</text>", 
                D(x), D(y), font.Name, D(font.Size), GetColor(C), anchor, text));
        }

        public override void DrawArc(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h, float startAngle, float sweepAngle)
        {
             // Arc in SVG is path 'A' command. Complex.
             // Placeholder:
        }

        public override void TranslateTransform(float x, float y)
        {
            // SVG transform group?
            // Ignoring for now or implementing if critical
            Transform = Transform * Matrix3x2.CreateTranslation(x, y);
        }

        public override void ScaleTransform(float sx, float sy)
        {
            Transform = Transform * Matrix3x2.CreateScale(sx, sy);
        }

        public override void RotateTransform(float angle)
        {
            Transform = Transform * Matrix3x2.CreateRotation(angle * (float)(Math.PI/180.0));
        }

        string GetColor(Color c)
        {
            var p = c.ToPixel<Rgba32>();
            return String.Format("rgba({0},{1},{2},{3})", p.R, p.G, p.B, p.A / 255.0);
        }

        string GetCMYK(double c, double m, double y, double k)
        {
             // CMYK to RGB conversion roughly
             double r = 255 * (1-c) * (1-k);
             double g = 255 * (1-m) * (1-k);
             double b = 255 * (1-y) * (1-k);
             return String.Format("rgb({0},{1},{2})", (int)r, (int)g, (int)b);
        }
        
        string D(double d)
        {
            return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }


        public SVGGraphicsInterface(double width, double height)
        {
           Width = width;
           Height = height;
        }

        public SVGGraphicsInterface() : base()
        {
        }

        public void DrawPolyline(GerberLibrary.Core.Primitives.Pen p, List<PointF> TheList, bool closed = false, bool clipagainstboundary = false, float strokewidth = -1)
        {
            string commands = "";

            Vector2 scalePt = new Vector2(0, p.Width);
            scalePt = Vector2.Transform(scalePt, Transform);
            
            scalePt.X -= Transform.M31;
            scalePt.Y -= Transform.M32;

            double stroke = strokewidth;
            if (stroke<=-1) stroke = Math.Sqrt(scalePt.X * scalePt.X + scalePt.Y* scalePt.Y);
            
            var list = TheList.Select(pt => new Vector2(pt.X, pt.Y)).ToArray();
            
            // Transform points
            for(int i=0; i<list.Length; i++)
            {
                list[i] = Vector2.Transform(list[i], Transform);
            }

            if (clipagainstboundary)
            {
                int clipped = 0;
                foreach (var a in list)
                {
                    if (a.X<0|| a.Y<0 || a.X> Width || a.Y>Height)
                    {
                        clipped++;
                    }
                }
                if (clipped == list.Count())
                {
                    return;
                }
            }

            commands += "M" + list[0].X.ToString().Replace(',', '.') + "," + list[0].Y.ToString().Replace(',', '.');
            for (int i = 1; i < list.Count(); i++)
            {
                commands += "L" + list[i].X.ToString().Replace(',', '.') + "," + list[i].Y.ToString().Replace(',', '.');
            }
            if (closed) commands += "Z";
            
            string setup = String.Format("<path fill=\"none\" stroke=\"{2}\" stroke-width=\"{0}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"{1}\"/>", stroke.ToString().Replace(',', '.'), commands, GetColor(p.Color));
            OutputLines.Add(setup);
        }

        private void FillPolyline(Color C, List<PointF> TheList, bool closed = false)
        {
            string commands = "";

            var list = TheList.Select(pt => new Vector2(pt.X, pt.Y)).ToArray();
            for(int i=0; i<list.Length; i++)
            {
                list[i] = Vector2.Transform(list[i], Transform);
            }

            commands += "M" + list[0].X.ToString().Replace(',', '.') + "," + list[0].Y.ToString().Replace(',', '.');
            for (int i = 1; i < list.Count(); i++)
            {
                commands += "L" + list[i].X.ToString().Replace(',', '.') + "," + list[i].Y.ToString().Replace(',', '.');
            }
            if (closed) commands += "Z";

            string setup = String.Format("<path fill=\"{1}\" d=\"{0}\"/>", commands, GetColor(C));
            OutputLines.Add(setup);
        }

        public void DrawLine(GerberLibrary.Core.Primitives.Pen P, float x1, float y1, float x2, float y2)
        {
            DrawLine(P, new PointF(x1, y1), new PointF(x2, y2));
        }

        public void DrawRectangle(Color color, float x, float y, float w, float h)
        {
            DrawRectangle(new Pen(color), x, y,w, h);
        }

        public void DrawRectangle(Color color, float x, float y, float w, float h, float strokewidth = 1)
        {
            DrawRectangle(new Pen(color, strokewidth), x, y,w, h);
        }

        public void DrawString(PointD pos, string text, double scale, bool centered, float r = 0.2F, float g = 0.2F, float b = 0.2F, float a = 1)
        {
           OutputLines.Add(String.Format("<!-- Text: {0} at {1},{2} -->", text, pos.X, pos.Y));
        }

        public void FillRectangle(Color color, float x, float y, float w, float h)
        {
            FillRectangle(new SolidBrush(color), x, y, w, h);
        }

        public void FillShape(GerberLibrary.Core.Primitives.SolidBrush BR, PolyLine Shape)
        {
             // Implement using FillPolygon
             // Convert PolyLine vertices to PointF array
             // Shape.Vertices is List<PointD>
             var pts = Shape.Vertices.Select(v => new PointF((float)v.X, (float)v.Y)).ToArray();
             FillPolygon(BR, pts);
        }

        public override PointD MeasureString(string p)
        {
            return new PointD(p.Length * 10, 15);
        }

        public void Save(string fileName)
        {
            List<string> fileout = new List<string>();
            fileout.Add("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\" >");
            fileout.Add(String.Format("<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xml:space=\"preserve\" width=\"{0}\" height=\"{1}\">", Width, Height));
            fileout.AddRange(OutputLines);
            fileout.Add("</svg>");
            System.IO.File.WriteAllLines(fileName, fileout);
        }


        public void WriteOutline()
        {
            List<PointF> Points = new List<PointF>();
            Points.Add(new PointF(0, 0));
            Points.Add(new PointF((float)Width, 0));
            Points.Add(new PointF((float)Width, (float)Height));
            Points.Add(new PointF(0, (float)Height));
            DrawPolyline(new Pen(Color.Black, 0.25f), Points, true);
        }

        public void FillTriangles(List<GerberLibrary.Core.Primitives.Triangle> triangles, Color C)
        {
            // Placeholder
        }
    }

    class SVGWriter
    {

        
        public static void Write(string filename, int w, int h, List<GerberLibrary.Core.Primitives.PolyLine> Polygons, double strokewidth)
        {
            List<string> OutLines = new List<string>();

            OutLines.Add("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\" >");
            OutLines.Add(String.Format("<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xml:space=\"preserve\" width=\"{0}\" height=\"{1}\">", w, h));
            Dictionary<int, List<string>> groups = new Dictionary<int, List<string>>();
            for(int i =0;i<20;i++)
            {
                groups[i] = new List<string>();
            }

            List<string> colors = new List<string>();// { "#606060", "#505050", "#404040", "#303030", "#202020", "#101010", "#080808", "#040404", "#020202", "#010101", "#000000", "#000000", "#000000", "#000000", "#000000", "#000000", "#000000", "#000000", "#000000" };

            for (int i =0;i<45;i++)
            {
                byte r = (byte)(Math.Sin(i * 3.0) * 127 + 127);
                byte g = (byte)(Math.Sin(2+ i * 3.0) * 127 + 127);
                byte b = (byte)(Math.Sin(4 +i * 3.0) * 127 + 127);
                colors.Add(String.Format("#{0:X2}{1:X2}{2:X2}", r, g, b));
            }

            foreach (var a in Polygons)
            {
                string commands = "";
                commands += "M" + a.Vertices[0].X.ToString().Replace(',', '.') + "," + a.Vertices[0].Y.ToString().Replace(',', '.');
                for (int i = 1; i < a.Vertices.Count; i++)
                {
                    commands += "L" + a.Vertices[i].X.ToString().Replace(',', '.') + "," + a.Vertices[i].Y.ToString().Replace(',', '.');
                }
                commands += "L" + a.Vertices[0].X.ToString().Replace(',','.') + "," + a.Vertices[0].Y.ToString().Replace(',', '.');
                commands += "Z";
                string setup = String.Format("<path fill=\"none\" stroke=\"{2}\" stroke-width=\"{0}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"{1}\"/>", strokewidth, commands, colors[0]);

                groups[0].Add(setup);
            }

            foreach(var a in groups)
            {
                var L = a.Value;
                if (L.Count > 0)
                {
                    OutLines.Add("<g>");
                    foreach (var p in L) OutLines.Add(p);
                    OutLines.Add("</g>");
                }
            }
            OutLines.Add("</svg>");
            System.IO.File.WriteAllLines(filename, OutLines);
        }
    }
}
