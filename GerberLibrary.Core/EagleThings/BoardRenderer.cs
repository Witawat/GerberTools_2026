using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System.Numerics;

using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;
using Pen = GerberLibrary.Core.Primitives.Pen;
using SolidBrush = GerberLibrary.Core.Primitives.SolidBrush;
using GraphicsPath = GerberLibrary.Core.Primitives.GraphicsPath;
using FontFamily = GerberLibrary.Core.Primitives.FontFamily;
using FontStyle = GerberLibrary.Core.Primitives.FontStyle;
using StringFormat = GerberLibrary.Core.Primitives.StringFormat;

using GerberLibrary.EagleXML;

namespace GerberLibrary
{
    namespace Eagle
    {
        public class ShapeInstance
        {
            public ShapeContainer TheShape;
            public bool TopSide = true;
            public PointF Position = new PointF(0, 0);
            public double Rotation = 0;

            public Bounds Bounds;
            internal PointF Centroid;

            public EagleLoader.DevicePlacement Placement;
        }

        public class Layer
        {
        }

        public class Bounds
        {
            public double minx;
            public double miny;
            public double maxx;
            public double maxy;
            public int fitpoints = 0;
            public void Fit(double x, double y)
            {
                if (fitpoints == 0)
                {
                    minx = maxx = x;
                    miny = maxy = y;
                }
                else
                {
                    if (x < minx) minx = x; else if (x > maxx) maxx = x;
                    if (y < miny) miny = y; else if (y > maxy) maxy = y;
                }
                fitpoints++;
            }

            public double Width()
            {
                return maxx - minx;
            }

            public void Clear()
            {
                fitpoints = 0;
            }

            public double Height()
            {
                return maxy - miny;
            }


        }

        public class BoardRenderer
        {
            public static Color SilkColor = Color.White;
            public static Color SMTColor = Color.DarkGray;
            public static Color PADColor = Color.DarkGreen;
            public static Color CopperColor = Color.Gold;
            public static Color SymbolsColor = Color.DarkRed;
            public static Bounds GetShapeBounds(ShapeContainer a)
            {
                Bounds R = new Bounds();
                foreach (var s in a.Shapes)
                {
                    var Ps = s.GetPolygons();
                    foreach (var P in Ps)
                    {
                        foreach (var v in P)
                        {
                            R.Fit(v.X, v.Y);
                        }
                    }
                }
                return R;

            }

            public static Color DrillColor = Color.Black;
            public static Color DocuColor = Color.FromRgba(255, 255, 0, 200); // ImageSharp Color.FromRgba(r,g,b,a)

            Dictionary<LayerSpec, Color> Colors = new Dictionary<LayerSpec, Color>();
            Dictionary<LayerSpec, int> LayerPriority = new Dictionary<LayerSpec, int>();
            Dictionary<LayerSpec, List<DisplayString>> StringsPerLayer = new Dictionary<LayerSpec, List<DisplayString>>();
            List<PointF> debugpoints = new List<PointF>();
            Dictionary<LayerSpec, int> Layers = new Dictionary<LayerSpec, int>();
            Dictionary<LayerSpec, List<List<Tuple<SchematicNet, List<PointF>>>>> ShapeLists = new Dictionary<LayerSpec, List<List<Tuple<SchematicNet, List<PointF>>>>>();

            public GerberLibrary.Bounds Bounds = new GerberLibrary.Bounds();
            public float GetScale(double MaxW, double MaxH)
            {
                double aspect = MaxW / MaxH;
                double dispw = Bounds.Width();
                double disph = Bounds.Height();
                double scale = 1.0;
                double partaspect = dispw / disph;

                if (partaspect > aspect)
                {
                    scale = (MaxW) / (dispw + 3);
                }
                else
                {
                    scale = (MaxH) / (disph + 3);
                }

                return (float)scale;
            }

            public Matrix3x2 Getmatrix(double MaxW, double MaxH, double scalefac = 1.0, double xoff = 0, double yoff = 0)
            {

                double minx = Bounds.TopLeft.X;
                double maxx = Bounds.BottomRight.X;
                double miny = Bounds.TopLeft.Y;
                double maxy = Bounds.BottomRight.Y;

                double aspect = MaxW / MaxH;
                double dispw = maxx - minx;
                double disph = maxy - miny;
                double scale = 1.0;
                double partaspect = dispw / disph;

                if (partaspect > aspect)
                {
                    scale = (MaxW) / (dispw + 3);
                }
                else
                {
                    scale = (MaxH) / (disph + 3);
                }

                // Reverse order of GDI+ operations for Matrix3x2
                Matrix3x2 mat = Matrix3x2.CreateScale((float)scalefac) *
                                Matrix3x2.CreateTranslation((float)xoff, (float)yoff) *
                                Matrix3x2.CreateTranslation((float)(-(maxx + minx) / 2), (float)((-(maxy + miny) / 2))) *
                                Matrix3x2.CreateScale((float)scale, (float)-scale) *
                                Matrix3x2.CreateTranslation((float)(MaxW / 2), (float)(MaxH / 2));
                                
                return mat;
            }

            public void DrawInstances(GerberLibrary.Core.GraphicsInterface g, double MaxW, double MaxH, bool rendergrid, Color gridcolor, double scalefac = 1.0, double xoff = 0, double yoff = 0)
            {
                double minx = Bounds.TopLeft.X;
                double maxx = Bounds.BottomRight.X;
                double miny = Bounds.TopLeft.Y;
                double maxy = Bounds.BottomRight.Y;


                double aspect = MaxW / MaxH;
                double dispw = maxx - minx;
                double disph = maxy - miny;
                double scale = 1.0;
                double partaspect = dispw / disph;

                if (partaspect > aspect)
                {
                    scale = (MaxW) / (dispw + 3);
                }
                else
                {
                    scale = (MaxH) / (disph + 3);
                }

                if (dispw == 0 || disph == 0) return;
                var originaltransform = g.Transform;


                g.TranslateTransform((float)((MaxW) / 2), (float)((MaxH) / 2));
                g.ScaleTransform((float)scale, (float)-scale);


                g.TranslateTransform((float)(-(maxx + minx) / 2), (float)((-(maxy + miny) / 2)));

                g.TranslateTransform((float)xoff, (float)yoff);
                g.ScaleTransform((float)scalefac, (float)scalefac);


                if (rendergrid)
                {
                    Pen Thick = new Pen(gridcolor, (float)(2.0 / scale));
                    Pen Thin = new Pen(gridcolor, (float)(1.0 / scale));
                    for (int x = (int)minx - 10; x < (int)maxx + 10; x++)
                    {
                        g.DrawLine((x % 5 == 0) ? Thick : Thin, new PointF(x, (float)miny - 10), new PointF(x, (float)maxy + 10));
                    }
                    for (int y = (int)miny - 10; y < (int)maxy + 10; y++)
                    {
                        g.DrawLine((y % 5 == 0) ? Thick : Thin, new PointF((float)minx - 10, (float)y), new PointF((float)maxx + 10, (float)y));
                    }
                }
                
                // StringFormat SF = new StringFormat();



                foreach (var l in (from i in Layers orderby i.Value select i.Key))
                {
                    var L = ShapeLists[l];

                    Color C = Color.FromRgba(0, 10, 255, 100);
                    if (Colors.ContainsKey(l))
                    {
                        var c = Colors[l];
                        // C = Color.FromArgb(200, c); // alpha, basecolor
                        C = Color.FromRgba(c.ToPixel<Rgba32>().R, c.ToPixel<Rgba32>().G, c.ToPixel<Rgba32>().B, 200);
                    }
                    foreach (var p in L)
                    {
                        if (p.Count > 0)
                        {
                            //    g.DrawPolygon(new Pen(C, (float)(1.0 / scale)), p.ToArray());
                            PathBuilder pb = new PathBuilder();
                            foreach (var P in p)
                            {
                                if (P.Item2.Count > 2)
                                    pb.AddLines(P.Item2.Select(pt => new PointF(pt.X, pt.Y)).ToArray());
                            }
                            g.FillPath(new SolidBrush(C), pb.Build());
                        }
                    }
                    foreach (var S in StringsPerLayer[l])
                    {
                        TextOptions options = new TextOptions(SystemFonts.CreateFont("Arial", (float)S.size));
                        
                        switch (S.alignment)
                        {
                            case textAlign.center: options.HorizontalAlignment = HorizontalAlignment.Center; options.VerticalAlignment = VerticalAlignment.Center; break;
                            case textAlign.centerleft: options.HorizontalAlignment = HorizontalAlignment.Left; options.VerticalAlignment = VerticalAlignment.Center; break;
                            case textAlign.centerright: options.HorizontalAlignment = HorizontalAlignment.Right; options.VerticalAlignment = VerticalAlignment.Center; break;
                            case textAlign.topcenter: options.HorizontalAlignment = HorizontalAlignment.Center; options.VerticalAlignment = VerticalAlignment.Top; break;
                            case textAlign.topleft: options.HorizontalAlignment = HorizontalAlignment.Left; options.VerticalAlignment = VerticalAlignment.Top; break;
                            case textAlign.topright: options.HorizontalAlignment = HorizontalAlignment.Right; options.VerticalAlignment = VerticalAlignment.Top; break;
                            case textAlign.bottomcenter: options.HorizontalAlignment = HorizontalAlignment.Center; options.VerticalAlignment = VerticalAlignment.Bottom; break;
                            case textAlign.bottomleft: options.HorizontalAlignment = HorizontalAlignment.Left; options.VerticalAlignment = VerticalAlignment.Bottom; break;
                            case textAlign.bottomright: options.HorizontalAlignment = HorizontalAlignment.Right; options.VerticalAlignment = VerticalAlignment.Bottom; break;
                        }
                        options.Origin = new PointF((float)S.x, (float)S.y);
                        g.DrawString(S.Text, options.Font, new SolidBrush(C), options.Origin, options);
                    }

                }
                
                foreach (var a in debugpoints)
                {
                    g.DrawRectangle(new Pen(Color.GreenYellow, 0.01f), a.X - .01f, a.Y - .01f, .02f, .02f);
                }

                g.Transform = originaltransform;
                g.TranslateTransform((float)((MaxW) / 2), (float)((MaxH) / 2));
                g.ScaleTransform((float)scale, (float)scale);
                g.TranslateTransform((float)(-(maxx + minx) / 2), (float)((-(maxy + miny) / 2)));

                // g.InterpolationMode = InterpolationMode.High;


                if (false)
                    foreach (var l in (from i in Layers orderby i.Value select i.Key))
                    {
                        var L = ShapeLists[l];

                        foreach (var p in L)
                        {
                            foreach (var P in p)
                            {
                                if (P.Item1 != null && P.Item1.Name != null)
                                {
                                    var PS = PolyCenter(P.Item2);

                                    GraphicsPath PATH = new GraphicsPath();
                                    PATH.AddString(
                                        P.Item1.Name,             // text to draw
                                        FontFamily.GenericSansSerif,  // or any other font family
                                        (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                                        72 * 10.0f / (72 * (float)scale),       // em size
                                        new PointF(PS.X, (float)maxy - (PS.Y - (float)miny)),              // location where to draw text
                                        new StringFormat());          // set options here (e.g. center alignment)
                                    g.DrawPath(new Pen(Color.Black, 2.0f / (float)scale), new PathBuilder().Build());
                                    g.FillPath(new SolidBrush(Color.White), new PathBuilder().Build());

                                    //g.DrawString(P.Item1.Name, new Font("Arial", 0.2f), Brushes.Black,, new StringFormat());


                                }
                            }
                        }
                    }
            }

            private PointF PolyCenter(List<PointF> points)
            {

                if (points.Count > 0)
                {
                    float x = 0;
                    float y = 0;
                    for (int i = 0; i < points.Count; i++)
                    {
                        x += points[i].X;
                        y += points[i].Y;
                    }
                    x /= (float)points.Count;
                    y /= (float)points.Count;
                    return new PointF(x, y);

                }
                return new PointF(0, 0);
            }

            public void PrepareInstances(List<ShapeInstance> Instances, BoardSpec BS, bool showtext = true)
            {
                Colors[LayerSpec.ParseLayer("PADS", BS.Layers, LayerType.Copper)] = PADColor;
                Colors[LayerSpec.ParseLayer("1", BS.Layers)] = CopperColor;
                Colors[LayerSpec.ParseLayer("TOPCOPPER", BS.Layers, LayerType.Copper)] = CopperColor;
                Colors[LayerSpec.ParseLayer("BOTHCOPPER", BS.Layers, LayerType.Copper)] = CopperColor;
                Colors[LayerSpec.ParseLayer("BOTTOMCOPPER", BS.Layers, LayerType.Copper)] = CopperColor;
                Colors[LayerSpec.ParseLayer("DRILL", BS.Layers)] = DrillColor;
                Colors[LayerSpec.ParseLayer("DRILLS", BS.Layers)] = DrillColor;
                /*Colors[LayerSpec.ParseLayer("16", BS.Layers)] = CopperColor;
                Colors[LayerSpec.ParseLayer("21", BS.Layers)] = SilkColor;
                Colors[LayerSpec.ParseLayer("51", BS.Layers)] = DocuColor;
                Colors[LayerSpec.ParseLayer("52", BS.Layers)] = DocuColor;
                Colors[LayerSpec.ParseLayer("22", BS.Layers)] = SilkColor;
                Colors[LayerSpec.ParseLayer("25", BS.Layers)] = SilkColor;
                Colors[LayerSpec.ParseLayer("26", BS.Layers)] = SilkColor;
                Colors[LayerSpec.ParseLayer("27", BS.Layers)] = SilkColor;
                Colors[LayerSpec.ParseLayer("28", BS.Layers)] = SilkColor;
                Colors[LayerSpec.ParseLayer("94", BS.Layers)] = SymbolsColor;*/
                foreach (var a in BS.Layers)
                {
                    switch (a.type)
                    {
                        case LayerType.Copper: Colors[a] = CopperColor; break;
                        case LayerType.Silk: Colors[a] = SilkColor; break;
                        case LayerType.Docu: Colors[a] = DocuColor; break;
                        case LayerType.Drill: Colors[a] = DrillColor; break;

                    }
                }

                LayerPriority[LayerSpec.ParseLayer("21", BS.Layers)] = 100;
                LayerPriority[LayerSpec.ParseLayer("22", BS.Layers)] = 100;
                LayerPriority[LayerSpec.ParseLayer("25", BS.Layers)] = 100;
                LayerPriority[LayerSpec.ParseLayer("26", BS.Layers)] = 100;
                LayerPriority[LayerSpec.ParseLayer("27", BS.Layers)] = 100;
                LayerPriority[LayerSpec.ParseLayer("28", BS.Layers)] = 100;
                LayerPriority[LayerSpec.ParseLayer("51", BS.Layers)] = 101;
                LayerPriority[LayerSpec.ParseLayer("52", BS.Layers)] = 101;
                LayerPriority[LayerSpec.ParseLayer("DRILL", BS.Layers)] = 200;
                LayerPriority[LayerSpec.ParseLayer("DRILLS", BS.Layers)] = 200;
                LayerPriority[LayerSpec.ParseLayer("TOPCOPPER", BS.Layers, LayerType.Copper)] = 90;
                LayerPriority[LayerSpec.ParseLayer("BOTTOMCOPPER", BS.Layers, LayerType.Copper)] = 90;
                LayerPriority[LayerSpec.ParseLayer("BOTHCOPPER", BS.Layers, LayerType.Copper)] = 90;

                int count = 0;
                int vertexcount = 0;
                foreach (var I in Instances)
                {
                    if (I.Bounds == null) I.Bounds = GetInstanceBounds(I);
                    var ThePackage = I.TheShape;
                    foreach (var a in ThePackage.Shapes)
                    {
                        LayerSpec layer = a.GetLayer();
                        if (Layers.ContainsKey(layer) == false)
                        {
                            Layers[layer] = 1;
                            if (LayerPriority.ContainsKey(layer)) Layers[layer] = LayerPriority[layer];
                            ShapeLists[layer] = new List<List<Tuple<SchematicNet, List<PointF>>>>();
                            count++;
                        }
                    }


                    Matrix3x2 R = Matrix3x2.Identity;
                    R = Matrix3x2.CreateTranslation((float)I.Position.X, (float)I.Position.Y) * R;
                    if (I.TopSide == false)
                    {
                        R = Matrix3x2.CreateScale(-1, 1) * R;
                    }
                    R = Matrix3x2.CreateRotation((float)(I.Rotation * Math.PI / 180.0)) * R;

                    foreach (var a in ThePackage.Shapes)
                    {

                        SchematicNet ShapeSignal = null;
                        if (I.Placement != null && I.Placement.Pins.ContainsKey(a.Name))
                        {
                            ShapeSignal = I.Placement.Pins[a.Name].signal;
                        }

                        var DBP = a.GetDebugPoints().ToArray();

                        var PolySet = new List<Tuple<SchematicNet, List<PointF>>>();
                        bool process = true;
                        if (a as TextShape != null && showtext == false)
                        { process = false; }
                        if (process)
                            foreach (var Ps in a.GetPolygons())
                            {
                                double minx = 0;
                                double miny = 0;
                                double maxx = 0;
                                double maxy = 0; ;

                                var P = Ps.ToArray();
                                for (int i = 0; i < P.Count(); i++)
                                {
                                    PointF rotP = new PointF(P[i].X, P[i].Y); 
                                    rotP = Vector2.Transform(rotP, R);
                                    P[i].X = rotP.X;
                                    P[i].Y = rotP.Y;
                                    Bounds.FitPoint(P[i].X, P[i].Y);
                                    if (vertexcount == 0)
                                    {
                                        minx = maxx = P[i].X;
                                        miny = maxy = P[i].Y;
                                    }
                                    else
                                    {
                                        if (P[i].X < minx) minx = P[i].X; else if (P[i].X > maxx) maxx = P[i].X;
                                        if (P[i].Y < miny) miny = P[i].Y; else if (P[i].Y > maxy) maxy = P[i].Y;

                                    }

                                    vertexcount++;
                                }
                                I.Centroid = new PointF((float)(minx + maxx) / 2, (float)(miny + maxy) / 2);
                                PolySet.Add(new Tuple<SchematicNet, List<PointF>>(ShapeSignal, P.ToList()));
                            }
                        if (StringsPerLayer.ContainsKey(a.GetLayer()) == false) StringsPerLayer[a.GetLayer()] = new List<DisplayString>();
                        foreach (var ds in a.DisplayStrings)
                        {
                            StringsPerLayer[a.GetLayer()].Add(new DisplayString() { alignment = ds.alignment, size = ds.size, x = ds.x + I.Position.X, Text = ds.Text, y = ds.y + I.Position.Y });
                        }
                        ShapeLists[a.GetLayer()].Add(PolySet);
                        for (int i = 0; i < DBP.Count(); i++)
                        {
                            DBP[i].X += I.Position.X;
                            DBP[i].Y += I.Position.Y;

                        }
                        debugpoints.AddRange(DBP.ToList());

                    }
                }

                if (count == 0) return;


                foreach (var l in ShapeLists.Values)
                {
                    foreach (var k in l)
                    {
                        foreach (var q in k)
                        {
                            foreach (var p in q.Item2)
                            {
                                Bounds.FitPoint(p.X, p.Y);
                            }
                        }
                    }
                }


            }

            private Bounds GetInstanceBounds(ShapeInstance i)
            {
                return GetShapeBounds(i.TheShape);
            }
        }
    }
}
