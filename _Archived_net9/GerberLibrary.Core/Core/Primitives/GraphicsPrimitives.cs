using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;

namespace GerberLibrary.Core.Primitives
{
    public struct Triangle
    {
        public PointD A;
        public PointD B;
        public PointD C;
    }

    public enum LineCap
    {
        Flat = 0,
        Square = 1,
        Round = 2,
        Triangle = 3
    }

    public enum LineJoin
    {
        Miter = 0,
        Bevel = 1,
        Round = 2,
        MiterClipped = 3
    }

    public struct Pen
    {
        public Color Color;
        public float Width;
        public LineCap StartCap;
        public LineCap EndCap;
        public LineJoin LineJoin;

        public Pen(Color color, float width = 1.0f)
        {
            Color = color;
            Width = width;
            StartCap = LineCap.Round;
            EndCap = LineCap.Round;
            LineJoin = LineJoin.Round;
        }
    }

    public struct SolidBrush
    {
        public Color Color;

        public SolidBrush(Color color)
        {
            Color = color;
        }
    }

    public enum StringAlignment
    {
        Near = 0,
        Center = 1,
        Far = 2
    }

    public class StringFormat
    {
        public StringAlignment Alignment { get; set; }
        public StringAlignment LineAlignment { get; set; }
    }

    public enum FontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikeout = 8
    }

    public class FontFamily
    {
        public static FontFamily GenericSansSerif = new FontFamily();
    }

    public class GraphicsPath
    {
        public List<PointF> PathPoints = new List<PointF>();
        public List<byte> PathTypes = new List<byte>();
        public int PointCount => PathPoints.Count;

        public void AddString(string s, FontFamily family, int style, float size, PointF location, StringFormat format)
        {
            // TODO: Implement using SixLabors.Fonts if needed
        }
    }

    public enum GraphicsInterpolationMode
    {
        Default = 0,
        Low = 1,
        High = 2,
        Bilinear = 3,
        Bicubic = 4,
        NearestNeighbor = 5,
        HighQualityBilinear = 6,
        HighQualityBicubic = 7
    }

    public enum CompositingMode
    {
        SourceOver = 0,
        SourceCopy = 1
    }
}
