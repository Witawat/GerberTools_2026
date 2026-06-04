using System;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using GerberLibrary.Core.Primitives;

namespace GerberLibrary.Core
{
    public abstract class GraphicsInterface
    {
        public abstract void Clear(Color color);
        public abstract void DrawLine(GerberLibrary.Core.Primitives.Pen pen, PointF p1, PointF p2);
        public abstract void DrawLines(GerberLibrary.Core.Primitives.Pen pen, PointF[] points);
        public abstract void DrawPolygon(GerberLibrary.Core.Primitives.Pen pen, PointF[] points);
        public abstract void FillPolygon(GerberLibrary.Core.Primitives.SolidBrush brush, PointF[] points);
        public abstract void FillRectangle(GerberLibrary.Core.Primitives.SolidBrush brush, float x, float y, float w, float h);
        public abstract void DrawRectangle(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h);
        public abstract void DrawImage(Image image, float x, float y, float w, float h);
        public abstract void DrawString(PointF location, string text, double size, bool center = false);
        public virtual void DrawString(PointD location, string text, double size, bool center = false)
        {
            DrawString(new PointF((float)location.X, (float)location.Y), text, size, center);
        }
        public abstract void DrawEllipse(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h);
        public abstract void FillEllipse(GerberLibrary.Core.Primitives.SolidBrush brush, float x, float y, float w, float h);
        public abstract void DrawArc(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h, float startAngle, float sweepAngle);
        public abstract void DrawPath(GerberLibrary.Core.Primitives.Pen pen, IPath path);
        public abstract void FillPath(GerberLibrary.Core.Primitives.SolidBrush brush, IPath path);
        public abstract void DrawString(string text, Font font, GerberLibrary.Core.Primitives.SolidBrush brush, PointF location, TextOptions options);
        public abstract PointD MeasureString(string text);
        public virtual void DrawString(PointD location, string text, double size, bool center, float r, float g, float b, float a)
        {
             DrawString(new PointF((float)location.X, (float)location.Y), text, size, center);
        }

        public virtual void DrawLine(GerberLibrary.Core.Primitives.Pen pen, float x1, float y1, float x2, float y2)
        {
            DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2));
        }

        public virtual void DrawRectangle(GerberLibrary.Core.Primitives.Pen pen, RectangleF rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public virtual void FillRectangle(GerberLibrary.Core.Primitives.SolidBrush brush, RectangleF rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public virtual void DrawEllipse(GerberLibrary.Core.Primitives.Pen pen, RectangleF rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public virtual void FillEllipse(GerberLibrary.Core.Primitives.SolidBrush brush, RectangleF rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }
        
        public abstract void TranslateTransform(float x, float y);
        public abstract void ScaleTransform(float sx, float sy);
        public abstract void RotateTransform(float angle);
        
        public abstract Matrix3x2 Transform { get; set; }
        public abstract RectangleF ClipBounds { get; }
        public abstract bool IsFast { get; }
        // Emulate System.Drawing InterpolationMode somewhat?
        public virtual GraphicsInterpolationMode InterpolationMode { get; set; } 
        public virtual CompositingMode CompositingMode { get; set; }
        public virtual bool AntiAlias { get; set; } = true;
    }

    public class ImageSharpGraphicsInterface : GraphicsInterface
    {
        private Image<Rgba32> _image;
        private Matrix3x2 _transform = Matrix3x2.Identity;

        public ImageSharpGraphicsInterface(Image<Rgba32> image)
        {
            _image = image;
        }

        public override Matrix3x2 Transform 
        { 
            get => _transform; 
            set => _transform = value; 
        }

        public override RectangleF ClipBounds => new RectangleF(0, 0, _image.Width, _image.Height);
        public override bool IsFast => false;
        public override GraphicsInterpolationMode InterpolationMode { get; set; }
        public override CompositingMode CompositingMode { get; set; }
        public override bool AntiAlias { get; set; } = true;

        public override void Clear(Color color)
        {
            _image.Mutate(x => x.BackgroundColor(color));
        }

        private DrawingOptions GetOptions()
        {
            return new DrawingOptions { Transform = _transform, GraphicsOptions = new GraphicsOptions { Antialias = AntiAlias } };
        }

        private SolidPen GetPen(GerberLibrary.Core.Primitives.Pen pen)
        {
            return new SolidPen(pen.Color, pen.Width);
        }

        public override void DrawLine(GerberLibrary.Core.Primitives.Pen pen, PointF p1, PointF p2)
        {
            _image.Mutate(x => x.DrawLine(GetOptions(), GetPen(pen), p1, p2));
        }
 
        public override void DrawLines(GerberLibrary.Core.Primitives.Pen pen, PointF[] points)
        {
            if (points.Length < 2) return;
            _image.Mutate(x => {
                for (int i = 0; i < points.Length - 1; i++)
                {
                    x.DrawLine(GetOptions(), GetPen(pen), points[i], points[i + 1]);
                }
            });
        }
 
        public override void DrawPolygon(GerberLibrary.Core.Primitives.Pen pen, PointF[] points)
        {
             _image.Mutate(x => x.DrawPolygon(GetOptions(), GetPen(pen), points));
        }
 
        public override void FillPolygon(GerberLibrary.Core.Primitives.SolidBrush brush, PointF[] points)
        {
            _image.Mutate(x => x.FillPolygon(GetOptions(), new SixLabors.ImageSharp.Drawing.Processing.SolidBrush(brush.Color), points));
        }
 
        public override void FillRectangle(GerberLibrary.Core.Primitives.SolidBrush brush, float x, float y, float w, float h)
        {
            _image.Mutate(ctx => ctx.Fill(GetOptions(), new SixLabors.ImageSharp.Drawing.Processing.SolidBrush(brush.Color), new RectangleF(x, y, w, h)));
        }
 
         public override void DrawRectangle(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h)
        {
            _image.Mutate(ctx => ctx.Draw(GetOptions(), GetPen(pen), new RectangleF(x, y, w, h)));
        }
 
        public override void DrawEllipse(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h)
        {
             var path = new EllipsePolygon(x + w / 2, y + h / 2, w / 2, h / 2);
             _image.Mutate(ctx => ctx.Draw(GetOptions(), GetPen(pen), path));
        }
 
        public override void FillEllipse(GerberLibrary.Core.Primitives.SolidBrush brush, float x, float y, float w, float h)
        {
             var path = new EllipsePolygon(x + w / 2, y + h / 2, w / 2, h / 2);
             _image.Mutate(ctx => ctx.Fill(GetOptions(), new SixLabors.ImageSharp.Drawing.Processing.SolidBrush(brush.Color), path));
        }
 
        public override void DrawArc(GerberLibrary.Core.Primitives.Pen pen, float x, float y, float w, float h, float startAngle, float sweepAngle)
        {
             var path = new PathBuilder().AddArc(new PointF(x + w / 2, y + h / 2), w / 2, h / 2, 0, startAngle, sweepAngle).Build();
             _image.Mutate(ctx => ctx.Draw(GetOptions(), GetPen(pen), path));
        }


        public override void DrawImage(Image image, float x, float y, float w, float h)
        {
             _image.Mutate(ctx => {
                 var scaled = image.Clone(ops => ops.Resize((int)w, (int)h));
                 ctx.DrawImage(scaled, new Point((int)x, (int)y), 1.0f);
             });
        }

        public override void DrawString(PointF location, string text, double size, bool center = false)
        {
             var font = SystemFonts.CreateFont("Arial", (float)size); 
             _image.Mutate(ctx => ctx.DrawText(GetOptions(), text, font, Color.White, location));
        }

        public override void TranslateTransform(float x, float y)
        {
            _transform = Matrix3x2.CreateTranslation(x, y) * _transform;
        }

        public override void ScaleTransform(float sx, float sy)
        {
             _transform = Matrix3x2.CreateScale(sx, sy) * _transform;
        }

        public override void DrawPath(GerberLibrary.Core.Primitives.Pen pen, IPath path)
        {
             _image.Mutate(ctx => ctx.Draw(GetOptions(), GetPen(pen), path));
        }
 
        public override void FillPath(GerberLibrary.Core.Primitives.SolidBrush brush, IPath path)
        {
             _image.Mutate(ctx => ctx.Fill(GetOptions(), new SixLabors.ImageSharp.Drawing.Processing.SolidBrush(brush.Color), path));
        }
 
        public override void DrawString(string text, Font font, GerberLibrary.Core.Primitives.SolidBrush brush, PointF location, TextOptions options)
        {
             _image.Mutate(ctx => ctx.DrawText(GetOptions(), text, font, brush.Color, location)); 
        }

        public override PointD MeasureString(string text)
        {
            var font = SystemFonts.CreateFont("Arial", 12);
            var size = TextMeasurer.MeasureSize(text, new TextOptions(font));
            return new PointD(size.Width, size.Height);
        }

        public override void RotateTransform(float angle)
        {
             _transform = Matrix3x2.CreateRotation((float)(angle * Math.PI / 180.0)) * _transform;
        }
    }
}
