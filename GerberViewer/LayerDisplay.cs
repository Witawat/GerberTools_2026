using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GerberLibrary.Core;
using GerberLibrary;
using GerberLibrary.Core.Primitives;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GerberViewer
{
    public partial class LayerDisplay : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        public LoadedStuff Document;
        public BoardSide DisplaySide;
        public LoadedStuff.DisplayGerber DispGerb;
        Dictionary<ParsedGerber, GerberVBO> FileVBOs = new Dictionary<ParsedGerber, GerberVBO>();
        HashSet<ParsedGerber> DirtyFiles = new HashSet<ParsedGerber>();
        private GerberViewerMainForm MainForm;
        ShaderProgram MainShader;

        public LayerDisplay(LoadedStuff doc, BoardSide Side, GerberViewerMainForm _Owner)
        {
            MainForm = _Owner;
            DisplaySide = Side;
            Document = doc;
            InitializeComponent();
            AddGLControl();
            CloseButtonVisible = false;
        }

        private void AddGLControl()
        {
            glcontrol1 = new OpenTK.GLControl(new OpenTK.Graphics.GraphicsMode(32, 24, 0, 4));
            glcontrol1.Dock = DockStyle.Fill;
            this.glcontrol1.Size = new System.Drawing.Size(632, 295);
            this.glcontrol1.TabIndex = 1;
            this.glcontrol1.VSync = false;
            this.glcontrol1.KeyDown += LayerDisplay_KeyDown;
            this.glcontrol1.KeyPress += LayerDisplay_KeyPress;
            this.glcontrol1.MouseEnter += pictureBox1_MouseEnter;
            this.glcontrol1.MouseMove += pictureBox1_MouseMove;
            this.glcontrol1.MouseLeave += pictureBox1_MouseLeave;
            this.glcontrol1.MouseDown += pictureBox1_MouseDown;
            this.glcontrol1.MouseUp += pictureBox1_MouseUp;
            this.glcontrol1.MouseWheel += LayerDisplay_MouseWheel;
            this.glcontrol1.MouseDoubleClick += LayerDisplay_MouseDoubleClick;

            glcontrol1.Paint += Glcontrol1_Paint;

            this.Controls.Add(glcontrol1);

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Layer Visibility...", null, (s, ev) => ShowLayerVisibilityForm());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Zoom to Fit", null, (s, ev) => PerformZoomToFit());
            contextMenu.Items.Add("Toggle Measure Mode", null, (s, ev) => ToggleMeasureMode());
            contextMenu.Items.Add("Export Viewport to PNG", null, (s, ev) => MainForm.ExportViewportToPng());
            glcontrol1.ContextMenuStrip = contextMenu;

        }


        string vert =
@"
#version 330
 
in vec3 vPosition;
in vec4 vColor;
in vec3 vOff;

out vec4 color;

uniform mat4 trans;
uniform mat4 view;
uniform float linescale;
 
void main()
{
    float s = 1.0;
    if (vColor.a < 0) s = 2; 
    gl_Position = view  * trans * vec4(vPosition + vOff * linescale * s, 1.0);
    color = vColor;
}";


        string frag =
@"
#version 330
 
in vec4 color;
out vec4 outputColor;
 
float checker(in float u, in float v)
{
    float checkSize = 3;
    float fmodResult = mod(floor(checkSize * u) + floor(checkSize * v), 2.0);

    if (fmodResult < 1.0) 
    {
        return 1.0;
    } 
    else 
    {
        return 0.0;
    }
}

void main()
{
    if (color.a< 0)
    {
        outputColor = vec4(color.xyz, -color.a);
    }
    else
    {
        outputColor = color;
    }
}

";
        private void EnsureGLInitialized()
        {
            if (glLoaded) return;
            lock (glInitLock)
            {
                if (glLoaded) return;
                if (glcontrol1 == null) return;
                glcontrol1.MakeCurrent();
                MainShader = new ShaderProgram(vert, frag, false);
                glLoaded = true;
            }
        }

        private void Glcontrol1_Paint(object sender, PaintEventArgs e)
        {
            EnsureGLInitialized();

            Bounds Bounds = new Bounds();
            foreach (var a in Document.Gerbers.OrderBy(x => x.sortindex))
            {
                Bounds.AddBox(a.File.BoundingBox);
            }

            GLGraphicsInterface GI = new GLGraphicsInterface(0, 0, Width, Height);
            glcontrol1.MakeCurrent();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Disable(EnableCap.CullFace);
            GL.Ortho(0, glcontrol1.Width, glcontrol1.Height, 0, -100, 100);
            GL.LineWidth(1.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Viewport(0, 0, glcontrol1.Width, glcontrol1.Height);

            Matrix4 View = Matrix4.CreateOrthographicOffCenter(0, glcontrol1.Width, glcontrol1.Height, 0, -100, 100);
            GI.Clear(Document.Colors.BackgroundColor);

            float S = GetScaleAndBuildTransform(GI, Bounds);
            currentScale = S;

            if (ShowGrid)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                DrawGrid(GI, S);
            }

            MainShader.Bind();
            var M = GI.GetGlMatrix();
            GL.Uniform1(MainShader.Uniforms["linescale"].address, 1.0f / S);
            GL.UniformMatrix4(MainShader.Uniforms["trans"].address, false, ref M);
            GL.UniformMatrix4(MainShader.Uniforms["view"].address, false, ref View);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (DispGerb == null)
            {
                bool isBottomView = DisplaySide == BoardSide.Bottom;
                var sideOrder = isBottomView
                    ? Document.Gerbers.OrderByDescending(x => x.sortindex)
                    : Document.Gerbers.OrderBy(x => x.sortindex);

                foreach (var a in sideOrder)
                {
                    int idx = Document.Gerbers.IndexOf(a);
                    if (!a.visible || HiddenLayerIndices.Contains(idx)) continue;
                    if (a.File.Layer == BoardLayer.Drill) continue;
                    if (a.File.Layer == BoardLayer.Outline || a.File.Layer == BoardLayer.Mill) continue;

                    bool isPrimarySide = a.File.Side == DisplaySide
                        || a.File.Side == BoardSide.Both
                        || a.File.Side == BoardSide.Internal
                        || a.File.Side == BoardSide.Unknown
                        || a.File.Side == BoardSide.Either;

                    var C = a.Color;
                    if (!isPrimarySide)
                        C = MathHelpers.Interpolate(C, Document.Colors.BackgroundColor, 0.4f);
                    int alpha = (int)(255 * a.Alpha);
                    alpha = Math.Max(20, Math.Min(255, alpha));
                    C = Color.FromArgb(alpha, C);
                    RebuildAndRender(a.File, C);
                }

                foreach (var a in Document.Gerbers.OrderBy(x => x.sortindex))
                {
                    int idx = Document.Gerbers.IndexOf(a);
                    if (!a.visible || HiddenLayerIndices.Contains(idx)) continue;
                    if (a.File.Layer != BoardLayer.Drill) continue;
                    int alpha = (int)(255 * a.Alpha);
                    RebuildAndRender(a.File, Color.FromArgb(alpha, a.Color));
                }

                foreach (var a in Document.Gerbers.OrderBy(x => x.sortindex))
                {
                    int idx = Document.Gerbers.IndexOf(a);
                    if (!a.visible || HiddenLayerIndices.Contains(idx)) continue;
                    if (a.File.Layer != BoardLayer.Mill) continue;
                    int alpha = (int)(255 * a.Alpha);
                    RebuildAndRender(a.File, Color.FromArgb(alpha, a.Color));
                }

                foreach (var a in Document.Gerbers.OrderBy(x => x.sortindex))
                {
                    int idx = Document.Gerbers.IndexOf(a);
                    if (!a.visible || HiddenLayerIndices.Contains(idx)) continue;
                    if (a.File.Layer != BoardLayer.Outline) continue;
                    int alpha = (int)(255 * a.Alpha);
                    RebuildAndRender(a.File, Color.FromArgb(alpha, a.Color));
                }
            }
            else
            {
                foreach (var a in Document.Gerbers.OrderBy(x => x.sortindex))
                {
                    int idx = Document.Gerbers.IndexOf(a);
                    if (!a.visible || HiddenLayerIndices.Contains(idx)) continue;
                    if (a.File.Layer == BoardLayer.Outline || a.File.Layer == BoardLayer.Mill)
                    {
                        int alpha = (int)(255 * 0.3f * a.Alpha);
                        RebuildAndRender(a.File, Color.FromArgb(alpha, a.Color), true);
                    }
                }

                int mainAlpha = (int)(255 * DispGerb.Alpha);
                RebuildAndRender(DispGerb.File, Color.FromArgb(mainAlpha, DispGerb.Color));
            }

            MainShader.UnBind();

            DrawMeasurementOverlays(GI, Bounds);

            if (Document.CrossHairActive && Document.Gerbers.Count > 0)
            {
                Color DimensionColor = Color.FromArgb(255, 255, 200);
                Pen P = new Pen(DimensionColor, 1.0f);
                P.DashPattern = new float[2] { 2, 2 };
                GI.DrawLine(P, (float)Bounds.TopLeft.X - 1000, Document.MouseY, (float)Bounds.BottomRight.X + 1000, Document.MouseY);
                GI.DrawLine(P, (float)Document.MouseX, (float)Bounds.TopLeft.Y - 1000, (float)Document.MouseX, (float)Bounds.BottomRight.Y + 1000);
            }

            glcontrol1.SwapBuffers();
            DrawMeasurementTextOverlay(e.Graphics, GI.Transform);
            MainForm.UpdateStatusBar(this);
        }

        public LayerDisplay(LoadedStuff doc, LoadedStuff.DisplayGerber Gerb, GerberViewerMainForm _Owner)
        {
            MainForm = _Owner;
            DispGerb = Gerb;
            DisplaySide = Gerb.File.Side;
            Document = doc;
            InitializeComponent();
            AddGLControl();
        }

        public float Zoomlevel = 1.0f;

        public PointF Offset = new PointF();
        private int lastY;
        private int lastX;
        private GLControl glcontrol1;
        private bool glLoaded = false;
        private object glInitLock = new object();
        private bool isPanning = false;
        private Point panStartMouse;
        private PointF panStartOffset;
        private float currentScale = 1.0f;

        public bool ShowGrid = true;
        public static bool SnapToGrid = true;
        public float GridSpacing = 1.0f;
        private Color GridColor = Color.FromArgb(70, 70, 75);

        public HashSet<int> HiddenLayerIndices = new HashSet<int>();

        public bool MeasureMode;
        private bool measureActive;
        private PointD measureStart;
        private PointD measureCurrent;
        private bool isPolyline;
        private List<PointD> polylinePoints = new List<PointD>();
        private double polylineTotal;
        private List<Tuple<PointD, PointD>> measurements = new List<Tuple<PointD, PointD>>();

        public double LastDistance { get; private set; }
        public double PolylineTotal { get { return polylineTotal; } }
        public bool PolylineMode { get { return isPolyline; } }

        public void UpdateDocument(bool force = false)
        {
            bool DoInvalidate = force;
            // if (this.DockPanel.Visible) { DoInvalidate = true; Console.Write("dockpanel visible - "); }
            if (this.DockPanel != null && this.DockPanel.ActiveDocument == this) { DoInvalidate = true; };// Console.Write("dockpane = this - "); }
            // if (this.Pane.IsActivated) { DoInvalidate = true; Console.Write("Activated - "); }
            //    if (this.Pane.IsActivePane) { DoInvalidate = true; Console.Write("ActivePane - "); }
            if (DispGerb == null) DoInvalidate = true;

            if (DoInvalidate)
            {
                if (DispGerb != null)
                {
                    //          Console.WriteLine("invalidating {0}", DispGerb.File);
                }
                else
                {
                    //Console.WriteLine("invalidating {0}", DisplaySide);
                }
                if (glcontrol1 != null) glcontrol1.Invalidate();
                //pictureBox1.Invalidate();
            }
            else
            {
                //  Console.WriteLine("Skipping ");
            }
        }



        private void DrawGerber(GerberVBO G, ParsedGerber file, Color C, bool dotted = false)
        {
            Pen P = new Pen(C, 1.0f);
            if (dotted) P.DashPattern = new float[2] { 2, 2 };
            SolidBrush B = new SolidBrush(C);
            var Out = file.IsOutline();
            G.Dotted = dotted;
            int Vbefore = G.VertexCount();
            foreach (var a in file.DisplayShapes)
            {
                if (a.Vertices.Count > 1)
                {
                    PointF[] Points = new PointF[a.Vertices.Count];
                    for (int i = 0; i < a.Vertices.Count; i++)
                    {
                        Points[i] = a.Vertices[i].ToF();
                    }
                    if (Out == false)
                    {
                        G.FillPath(C, Points);
                    }
                    else
                    {
                        G.DrawPath(C, Points, 1.0f, true);
                    }
                }
            }
            int Vafter = G.VertexCount();
            Console.WriteLine("Drawing file: {0} - {1} shapes, {2} vertices", file.Name, file.DisplayShapes.Count, Vafter - Vbefore);
        }

        internal void ClearCache(bool GeomChanged)
        {
            if (GeomChanged)
            {
                foreach (var vbo in FileVBOs.Values) vbo.Reset();
                FileVBOs.Clear();
                DirtyFiles.Clear();
            }
        }

        private GerberVBO GetOrCreateFileVBO(ParsedGerber file)
        {
            if (FileVBOs.TryGetValue(file, out var vbo))
                return vbo;
            vbo = new GerberVBO();
            FileVBOs[file] = vbo;
            DirtyFiles.Add(file);
            return vbo;
        }

        internal void MarkFileDirty(ParsedGerber file)
        {
            if (file != null) DirtyFiles.Add(file);
        }

        private void RebuildAndRender(ParsedGerber file, Color color, bool dotted = false)
        {
            var vbo = GetOrCreateFileVBO(file);
            if (DirtyFiles.Contains(file))
            {
                vbo.Reset();
                DrawGerber(vbo, file, color, dotted);
                vbo.BuildVBO();
                DirtyFiles.Remove(file);
            }
            vbo.RenderVBO(MainShader);
        }

        private float GetScaleAndBuildTransform(GraphicsInterface G2, Bounds Bounds)
        {
            Bitmap B = new Bitmap(1, 1);
            Graphics G = Graphics.FromImage(B);

            float S = GetScaleAndBuildTransform(G, Bounds);
            G2.Transform = G.Transform.Clone();

            return S;
        }


        private float GetScaleAndBuildTransform(Graphics G2, Bounds Bounds)
        {
            float S = 1;
            if (DisplaySide == BoardSide.Bottom)
            {
                S = Bounds.GenerateTransformWithScaleOffset(G2, Width, Height, 14, false, Zoomlevel, Offset);
            }
            else
            {
                S = Bounds.GenerateTransformWithScaleOffset(G2, Width, Height, 14, true, Zoomlevel, Offset);

            }

            return S;
        }

        private void DrawLabel(Graphics G, string TEXT, float S, float FontSize, Color C, int Xoff, int Yoff, float X, float Y, bool v5)
        {
            var T = G.Transform.Clone();

            G.TranslateTransform(X, Y);
            G.ScaleTransform((1 / S) * (v5 ? -1 : 1), -1 / S);

            G.DrawString(TEXT, new Font("Consolas", FontSize), new SolidBrush(C), Xoff, Yoff);

            G.Transform = T;

        }



        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            if (glcontrol1 != null) glcontrol1.Invalidate();
        }


        void SetXY(int x, int y)
        {
            lastX = x;
            lastY = y;
            if (Document.Gerbers.Count == 0) return;

            Bounds Bounds = new Bounds();
            foreach (var a in Document.Gerbers.OrderBy(xx => xx.sortindex))
                Bounds.AddBox(a.File.BoundingBox);

            PointF[] P = new PointF[1] { new PointF(x, y) };
            using (Graphics G = Graphics.FromImage(new Bitmap(1, 1)))
            {
                float S = GetScaleAndBuildTransform(G, Bounds);
                var M = G.Transform.Clone();
                M.Invert();
                M.TransformPoints(P);
            }

            if (SnapToGrid && GridSpacing > 0)
            {
                P[0].X = (float)(Math.Round(P[0].X / GridSpacing) * GridSpacing);
                P[0].Y = (float)(Math.Round(P[0].Y / GridSpacing) * GridSpacing);
            }

            MainForm.SetMouseCoord(P[0].X, P[0].Y);
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            Document.CrossHairActive = true;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                float dx = (e.X - panStartMouse.X) / currentScale;
                float dy = (e.Y - panStartMouse.Y) / currentScale;
                if (DisplaySide == BoardSide.Bottom)
                    Offset = new PointF(panStartOffset.X - dx, panStartOffset.Y - dy);
                else
                    Offset = new PointF(panStartOffset.X + dx, panStartOffset.Y - dy);
                ClearCache(false);
            }
            else if (MeasureMode && measureActive)
            {
                SetXY(e.X, e.Y);
                measureCurrent = new PointD(Document.MouseX, Document.MouseY);
            }

            if (!(MeasureMode && measureActive))
            {
                SetXY(e.X, e.Y);
            }
            else
            {
                if (glcontrol1 != null) glcontrol1.Invalidate();
            }

            if (MeasureMode && measureActive)
                MainForm.UpdateStatusBar(this);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && MeasureMode)
            {
                if (!measureActive)
                {
                    SetXY(e.X, e.Y);
                    measureActive = true;
                    measureStart = new PointD(Document.MouseX, Document.MouseY);
                    measureCurrent = measureStart;
                    isPolyline = (ModifierKeys == Keys.Shift);
                    if (isPolyline && polylinePoints.Count == 0)
                    {
                        polylinePoints.Add(measureStart);
                        polylineTotal = 0;
                    }
                }
                else
                {
                    SetXY(e.X, e.Y);
                    var endPt = new PointD(Document.MouseX, Document.MouseY);
                    var segDist = PointD.Distance(measureStart, endPt);
                    LastDistance = segDist;
                    if (isPolyline)
                    {
                        polylineTotal += segDist;
                        polylinePoints.Add(endPt);
                        measureStart = endPt;
                        measureCurrent = endPt;
                    }
                    else
                    {
                        measurements.Add(Tuple.Create(measureStart, endPt));
                        measureActive = false;
                    }
                }
                ClearCache(false);
                if (glcontrol1 != null) glcontrol1.Invalidate();
                MainForm.UpdateStatusBar(this);
                return;
            }

            if (e.Button == MouseButtons.Middle ||
               ((ModifierKeys & Keys.Space) != 0 && e.Button == MouseButtons.Left && !MeasureMode))
            {
                if (Document.Gerbers.Count > 0)
                {
                    isPanning = true;
                    panStartMouse = e.Location;
                    panStartOffset = Offset;
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left)
                isPanning = false;
        }

        private void LayerDisplay_MouseWheel(object sender, MouseEventArgs e)
        {
            bool ctrl = (ModifierKeys & Keys.Control) != 0;
            float factor = e.Delta > 0 ? (ctrl ? 1.03f : 1.1f) : (ctrl ? 0.97f : 0.9f);
            float newZoom = Zoomlevel * factor;
            float ratio = Zoomlevel / newZoom;
            Offset = new PointF(
                ratio * (Offset.X + Document.MouseX) - Document.MouseX,
                ratio * (Offset.Y + Document.MouseY) - Document.MouseY);
            Zoomlevel = newZoom;
            ClearCache(false);
            if (glcontrol1 != null) glcontrol1.Invalidate();
            MainForm.UpdateStatusBar(this);
        }

        private void LayerDisplay_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PerformZoomToFit();
            else if (e.Button == MouseButtons.Middle && MeasureMode && measureActive)
            {
                measureActive = false;
                if (glcontrol1 != null) glcontrol1.Invalidate();
                MainForm.UpdateStatusBar(this);
            }
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            MainForm.MouseOut();
            if (glcontrol1 != null) glcontrol1.Invalidate();
        }

        private void LayerDisplay_KeyDown(object sender, KeyEventArgs e)
        {
            bool invalidate = false;
            switch (e.KeyCode)
            {
                case Keys.Add: Zoomlevel *= 1.1f; invalidate = true; break;
                case Keys.Subtract: Zoomlevel *= 0.8f; invalidate = true; break;
            }

            if (invalidate)
            {
                //pictureBox1.Invalidate();
                if (glcontrol1 != null) glcontrol1.Invalidate();

            }
        }

        private void LayerDisplay_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            bool invalidate = false;
            switch (e.KeyChar)
            {
                case '+':
                    {
                        float NewZoomlevel = Zoomlevel * 1.1f;
                        Offset.X -= (Document.MouseX * NewZoomlevel) - (Document.MouseX * Zoomlevel);
                        Offset.Y -= (Document.MouseY * NewZoomlevel) - (Document.MouseY * Zoomlevel);
                        invalidate = true;
                        Zoomlevel = NewZoomlevel;
                    }
                    break;

                case '-':
                    {
                        float NewZoomlevel = Zoomlevel * 0.9f;
                        Offset.X -= (Document.MouseX * NewZoomlevel) - (Document.MouseX * Zoomlevel);
                        Offset.Y -= (Document.MouseY * NewZoomlevel) - (Document.MouseY * Zoomlevel);
                        invalidate = true;
                        Zoomlevel = NewZoomlevel;
                    }
                    break;
                case 'f':
                case 'F':
                    Zoomlevel = 1.0f; Offset.X = 0; Offset.Y = 0;
                    invalidate = true;
                    break;
            }

            if (invalidate)
            {
                SetXY(lastX, lastY);
                ClearCache(false);
               // pictureBox1.Invalidate();
                if (glcontrol1 != null) glcontrol1.Invalidate();

            }


        }

        private void LayerDisplay_Resize(object sender, EventArgs e)
        {
            if (glcontrol1 != null) glcontrol1.Invalidate();
        }

        private void ShowLayerVisibilityForm()
        {
            var form = new LayerVisibilityForm(this, Document);
            form.ShowDialog(this);
            foreach (var g in Document.Gerbers) MarkFileDirty(g.File);
            if (glcontrol1 != null) glcontrol1.Invalidate();
        }

        public void PerformZoomToFit()
        {
            Zoomlevel = 1.0f;
            Offset = new PointF(0, 0);
            if (glcontrol1 != null) glcontrol1.Invalidate();
            MainForm.UpdateStatusBar(this);
        }

        public void ZoomIn()
        {
            float newZoom = Zoomlevel * 1.3f;
            Offset = new PointF(
                Offset.X - (float)((Document.MouseX * newZoom) - (Document.MouseX * Zoomlevel)),
                Offset.Y - (float)((Document.MouseY * newZoom) - (Document.MouseY * Zoomlevel)));
            Zoomlevel = newZoom;
            ClearCache(false);
            if (glcontrol1 != null) glcontrol1.Invalidate();
            MainForm.UpdateStatusBar(this);
        }

        public void ZoomOut()
        {
            float newZoom = Zoomlevel * 0.7f;
            Offset = new PointF(
                Offset.X - (float)((Document.MouseX * newZoom) - (Document.MouseX * Zoomlevel)),
                Offset.Y - (float)((Document.MouseY * newZoom) - (Document.MouseY * Zoomlevel)));
            Zoomlevel = newZoom;
            ClearCache(false);
            if (glcontrol1 != null) glcontrol1.Invalidate();
            MainForm.UpdateStatusBar(this);
        }

        public void ToggleMeasureMode()
        {
            MeasureMode = !MeasureMode;
            if (!MeasureMode)
            {
                measureActive = false;
                measurements.Clear();
                polylinePoints.Clear();
                polylineTotal = 0;
                LastDistance = 0;
            }
            ClearCache(false);
            if (glcontrol1 != null) glcontrol1.Invalidate();
            MainForm.UpdateStatusBar(this);
        }

        public void ToggleGrid()
        {
            ShowGrid = !ShowGrid;
            if (glcontrol1 != null) glcontrol1.Invalidate();
        }

        public void ToggleSnap()
        {
            SnapToGrid = !SnapToGrid;
            if (glcontrol1 != null) glcontrol1.Invalidate();
        }

        public void SetGridSpacing(float val)
        {
            GridSpacing = val;
            if (glcontrol1 != null) glcontrol1.Invalidate();
        }

        public Bitmap CaptureViewport()
        {
            if (glcontrol1 == null || !glLoaded) return null;
            glcontrol1.MakeCurrent();
            Bitmap bmp = new Bitmap(glcontrol1.Width, glcontrol1.Height);
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, glcontrol1.Width, glcontrol1.Height),
                ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, glcontrol1.Width, glcontrol1.Height,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bmp;
        }

        private void DrawGrid(GLGraphicsInterface GI, float scale)
        {
            if (Document.Gerbers.Count == 0) return;

            float spacing = GridSpacing;
            float dotWorld = 2.5f / Math.Abs(scale);

            PointF[] corners = new PointF[4] {
                new PointF(0, 0),
                new PointF(glcontrol1.Width, 0),
                new PointF(glcontrol1.Width, glcontrol1.Height),
                new PointF(0, glcontrol1.Height)
            };
            var inv = GI.Transform.Clone();
            try { inv.Invert(); }
            catch { return; }
            inv.TransformPoints(corners);

            float left = corners[0].X, right = corners[0].X;
            float top = corners[0].Y, bottom = corners[0].Y;
            for (int i = 1; i < 4; i++)
            {
                left = Math.Min(left, corners[i].X);
                right = Math.Max(right, corners[i].X);
                top = Math.Min(top, corners[i].Y);
                bottom = Math.Max(bottom, corners[i].Y);
            }

            float vw = Math.Abs(right - left);
            float vh = Math.Abs(bottom - top);
            if (vw < 0.001f || vh < 0.001f) return;

            float estDots = (vw / spacing) * (vh / spacing);
            if (estDots > 2500)
            {
                float factor = (float)Math.Sqrt(estDots / 2500.0);
                spacing *= (float)Math.Ceiling(factor);
            }

            float startX = (float)(Math.Floor(left / spacing) * spacing);
            float startY = (float)(Math.Floor(top / spacing) * spacing);
            float endX = (float)(Math.Floor(right / spacing) * spacing);
            float endY = (float)(Math.Floor(bottom / spacing) * spacing);

            float hw = dotWorld * 0.5f;
            GL.Color4(GridColor);
            GL.Begin(PrimitiveType.Quads);
            for (float x = startX; x <= endX + spacing; x += spacing)
            {
                for (float y = startY; y <= endY + spacing; y += spacing)
                {
                    GL.Vertex2(x - hw, y - hw);
                    GL.Vertex2(x + hw, y - hw);
                    GL.Vertex2(x + hw, y + hw);
                    GL.Vertex2(x - hw, y + hw);
                }
            }
            GL.End();

            float originSize = 8.0f / Math.Abs(scale);
            float ohw = originSize * 0.5f;
            float crossExt = originSize * 1.5f;
            GL.Color4(Color.FromArgb(255, 200, 50));
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(0, -crossExt); GL.Vertex2(0, crossExt);
            GL.Vertex2(-crossExt, 0); GL.Vertex2(crossExt, 0);
            GL.End();

            float dotHw = originSize * 0.3f;
            GL.Color4(Color.FromArgb(255, 200, 50));
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex2(-dotHw, -dotHw);
            GL.Vertex2( dotHw, -dotHw);
            GL.Vertex2( dotHw,  dotHw);
            GL.Vertex2(-dotHw,  dotHw);
            GL.End();
        }

        private void DrawMeasurementOverlays(GLGraphicsInterface GI, Bounds Bounds)
        {
            if (!MeasureMode || Document.Gerbers.Count == 0) return;

            Pen measurePen = new Pen(Color.FromArgb(255, 255, 100), 1.0f);
            Pen pendingPen = new Pen(Color.FromArgb(200, 200, 255), 1.0f);
            pendingPen.DashPattern = new float[] { 4, 4 };

            foreach (var m in measurements)
                GI.DrawLine(measurePen, (float)m.Item1.X, (float)m.Item1.Y, (float)m.Item2.X, (float)m.Item2.Y);

            if (polylinePoints.Count > 1)
                for (int i = 0; i < polylinePoints.Count - 1; i++)
                    GI.DrawLine(measurePen, (float)polylinePoints[i].X, (float)polylinePoints[i].Y,
                        (float)polylinePoints[i + 1].X, (float)polylinePoints[i + 1].Y);

            if (measureActive)
                GI.DrawLine(pendingPen, (float)measureStart.X, (float)measureStart.Y,
                    (float)measureCurrent.X, (float)measureCurrent.Y);
        }

        private void DrawMeasurementTextOverlay(Graphics g, Matrix worldToScreen)
        {
            if (!MeasureMode || Document.Gerbers.Count == 0) return;
            if (worldToScreen == null) return;

            var font = new Font("Arial", 10, FontStyle.Bold);
            var brush = new SolidBrush(Color.FromArgb(255, 255, 100));
            var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));

            foreach (var m in measurements)
            {
                float mx = (float)(m.Item1.X + m.Item2.X) / 2;
                float my = (float)(m.Item1.Y + m.Item2.Y) / 2;
                PointF[] pts = new PointF[] { new PointF(mx, my) };
                worldToScreen.TransformPoints(pts);
                float dist = (float)PointD.Distance(m.Item1, m.Item2);
                string text = string.Format("{0:F2} mm", dist);
                var sz = g.MeasureString(text, font);
                g.DrawString(text, font, bgBrush, pts[0].X - sz.Width / 2 + 1, pts[0].Y - sz.Height / 2 + 1);
                g.DrawString(text, font, brush, pts[0].X - sz.Width / 2, pts[0].Y - sz.Height / 2);
            }

            if (polylinePoints.Count > 1)
            {
                for (int i = 0; i < polylinePoints.Count - 1; i++)
                {
                    float mx = (float)(polylinePoints[i].X + polylinePoints[i + 1].X) / 2;
                    float my = (float)(polylinePoints[i].Y + polylinePoints[i + 1].Y) / 2;
                    PointF[] pts = new PointF[] { new PointF(mx, my) };
                    worldToScreen.TransformPoints(pts);
                    float segDist = (float)PointD.Distance(polylinePoints[i], polylinePoints[i + 1]);
                    string text = string.Format("{0:F2} mm", segDist);
                    var sz = g.MeasureString(text, font);
                    g.DrawString(text, font, bgBrush, pts[0].X - sz.Width / 2 + 1, pts[0].Y - sz.Height / 2 + 1);
                    g.DrawString(text, font, brush, pts[0].X - sz.Width / 2, pts[0].Y - sz.Height / 2);
                }

                var last = polylinePoints.Last();
                PointF[] totalPt = new PointF[] { new PointF((float)last.X, (float)last.Y) };
                worldToScreen.TransformPoints(totalPt);
                string totalText = string.Format("Total: {0:F2} mm", polylineTotal);
                var totalSz = g.MeasureString(totalText, font);
                g.DrawString(totalText, font, bgBrush, totalPt[0].X - totalSz.Width / 2 + 1, totalPt[0].Y - totalSz.Height - 16 + 1);
                g.DrawString(totalText, font, brush, totalPt[0].X - totalSz.Width / 2, totalPt[0].Y - totalSz.Height - 16);
            }

            if (measureActive)
            {
                float mx = (float)(measureStart.X + measureCurrent.X) / 2;
                float my = (float)(measureStart.Y + measureCurrent.Y) / 2;
                PointF[] pts = new PointF[] { new PointF(mx, my) };
                worldToScreen.TransformPoints(pts);
                float dist = (float)PointD.Distance(measureStart, measureCurrent);
                string text = string.Format("{0:F2} mm", dist);
                var sz = g.MeasureString(text, font);
                g.DrawString(text, font, bgBrush, pts[0].X - sz.Width / 2 + 1, pts[0].Y - sz.Height / 2 + 1);
                g.DrawString(text, font, brush, pts[0].X - sz.Width / 2, pts[0].Y - sz.Height / 2);
            }

            font.Dispose();
            brush.Dispose();
            bgBrush.Dispose();
        }
    }
}
