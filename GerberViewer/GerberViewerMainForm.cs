using GerberLibrary;
using GerberLibrary.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;
using GerberViewer.Properties;

namespace GerberViewer
{
    public partial class GerberViewerMainForm : Form
    {
        private DockPanel dockPanel;
        LoadedStuff Document = new LoadedStuff();
        private ProgressLog _log;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel modeLabel;
        private ToolStripStatusLabel coordLabel;
        private ToolStripStatusLabel zoomLabel;
        private ToolStripStatusLabel measureLabel;
        private ToolStripButton measureButton;
        private ToolStripComboBox gridSpacingCombo;
        private ToolStripButton gridToggleButton;
        private ToolStripButton snapToggleButton;
        private ToolStripMenuItem recentFilesMenuItem;

        public GerberViewerMainForm(string[] args)
        {
            Gerber.ArcQualityScaleFactor = 20;

            InitializeComponent();

            _log = new StandardConsoleLog();

            this.dockPanel = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            var theme = new VS2015BlueTheme();
            this.dockPanel.Theme = theme;
            this.dockPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Controls.Add(this.dockPanel);

            BuildToolStrip();
            BuildStatusStrip();

            dockPanel.UpdateDockWindowZOrder(DockStyle.Left, true);
            ShowDockContent();

            List<String> files = new List<string>();
            foreach (string S in args)
            {
                if (Directory.Exists(S))
                {
                    LoadGerberFolder(Directory.GetFiles(S).ToList());
                }
                else
                {
                    if (File.Exists(S)) files.Add(S);
                }
            }
            if (files.Count > 0)
            {
                LoadGerberFolder(files);
            }
        }

        private LayerDisplay ActiveLayerDisplay
        {
            get
            {
                var ad = dockPanel.ActiveDocument;
                if (ad is LayerDisplay ld) return ld;
                return TheTopDisplay;
            }
        }

        LayerList TheList;
        LayerDisplay TheTopDisplay;
        LayerDisplay TheBottomDisplay;

        List<LayerDisplay> SingleLayers = new List<LayerDisplay>();

        private void BuildToolStrip()
        {
            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;

            var fileBtn = new ToolStripDropDownButton("&File");
            var openItem = new ToolStripMenuItem("&Open...", null, (s, e) => OpenFilesDialog());
            openItem.ShortcutKeys = Keys.Control | Keys.O;
            recentFilesMenuItem = new ToolStripMenuItem("Recent Files");
            var exitItem = new ToolStripMenuItem("E&xit", null, (s, e) => Close());
            fileBtn.DropDownItems.Add(openItem);
            fileBtn.DropDownItems.Add(recentFilesMenuItem);
            fileBtn.DropDownItems.Add(new ToolStripSeparator());
            fileBtn.DropDownItems.Add(exitItem);
            toolStrip.Items.Add(fileBtn);
            BuildRecentFilesMenu();

            var btnZoomIn = new ToolStripButton("Zoom In", null, (s, e) => ActiveLayerDisplay?.ZoomIn());
            var btnZoomOut = new ToolStripButton("Zoom Out", null, (s, e) => ActiveLayerDisplay?.ZoomOut());
            var btnZoomFit = new ToolStripButton("Zoom Fit", null, (s, e) => ActiveLayerDisplay?.PerformZoomToFit());

            measureButton = new ToolStripButton("Measure", null, (s, e) =>
            {
                var ld = ActiveLayerDisplay;
                if (ld != null)
                {
                    ld.ToggleMeasureMode();
                    measureButton.Checked = ld.MeasureMode;
                    UpdateStatusBar(ld);
                }
            });
            measureButton.CheckOnClick = true;

            var btnExportPng = new ToolStripButton("Export PNG", null, (s, e) => ExportViewportToPng());
            var btnClearAll = new ToolStripButton("Clear All", null, (s, e) => ClearAll());

            toolStrip.Items.Add(btnZoomIn);
            toolStrip.Items.Add(btnZoomOut);
            toolStrip.Items.Add(btnZoomFit);
            toolStrip.Items.Add(new ToolStripSeparator());

            gridToggleButton = new ToolStripButton("Grid", null, (s, e) =>
            {
                var ld = ActiveLayerDisplay;
                if (ld != null)
                {
                    ld.ToggleGrid();
                    gridToggleButton.Checked = ld.ShowGrid;
                    UpdateStatusBar(ld);
                }
            });
            gridToggleButton.CheckOnClick = true;
            gridToggleButton.Checked = true;

            gridSpacingCombo = new ToolStripComboBox("gridSpacing");
            gridSpacingCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            gridSpacingCombo.Items.AddRange(new object[] { "0.01", "0.1", "0.25", "0.5", "1.0", "2.5", "5.0", "10.0" });
            gridSpacingCombo.SelectedItem = "1.0";
            gridSpacingCombo.SelectedIndexChanged += (s, e) =>
            {
                var ld = ActiveLayerDisplay;
                if (ld != null && gridSpacingCombo.SelectedItem != null)
                {
                    if (float.TryParse(gridSpacingCombo.SelectedItem.ToString(), out float val))
                    {
                        ld.SetGridSpacing(val);
                    }
                }
            };

            snapToggleButton = new ToolStripButton("Snap", null, (s, e) =>
            {
                var ld = ActiveLayerDisplay;
                if (ld != null)
                {
                    ld.ToggleSnap();
                    snapToggleButton.Checked = ld.SnapToGrid;
                    UpdateStatusBar(ld);
                }
            });
            snapToggleButton.CheckOnClick = true;
            snapToggleButton.Checked = false;

            toolStrip.Items.Add(new ToolStripLabel(" Grid:"));
            toolStrip.Items.Add(gridSpacingCombo);
            toolStrip.Items.Add(gridToggleButton);
            toolStrip.Items.Add(snapToggleButton);
            toolStrip.Items.Add(new ToolStripSeparator());

            toolStrip.Items.Add(measureButton);
            toolStrip.Items.Add(btnExportPng);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(btnClearAll);

            this.Controls.Add(toolStrip);
        }

        internal void AddRecentFile(string path)
        {
            var recent = Settings.Default.RecentFiles;
            if (recent == null) recent = new StringCollection();
            recent.Remove(path);
            while (recent.Count >= 10) recent.RemoveAt(recent.Count - 1);
            recent.Insert(0, path);
            Settings.Default.RecentFiles = recent;
            try { Settings.Default.Save(); } catch { }
            BuildRecentFilesMenu();
        }

        private void BuildRecentFilesMenu()
        {
            recentFilesMenuItem.DropDownItems.Clear();
            var recent = Settings.Default.RecentFiles;
            if (recent != null && recent.Count > 0)
            {
                foreach (string file in recent)
                {
                    if (!File.Exists(file)) continue;
                    var item = new ToolStripMenuItem(file, null, (s, e) =>
                    {
                        LoadGerberFolder(new List<string> { file });
                        AddRecentFile(file);
                    });
                    recentFilesMenuItem.DropDownItems.Add(item);
                }
            }
            else
            {
                recentFilesMenuItem.DropDownItems.Add(new ToolStripMenuItem("(none)") { Enabled = false });
            }
        }

        private void OpenFilesDialog()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Title = "Select Gerber files";
                ofd.Filter = "All supported|*.gbr;*.gtl;*.gbl;*.gts;*.gbs;*.gto;*.gbo;*.gtp;*.gbp;*.gko;*.gm1;*.txt;*.drl|Gerber files (*.gbr)|*.gbr|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadGerberFolder(ofd.FileNames.ToList());
                    foreach (var f in ofd.FileNames) AddRecentFile(f);
                }
            }
        }

        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;

            modeLabel = new ToolStripStatusLabel("Navigate");
            modeLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            modeLabel.Width = 120;

            coordLabel = new ToolStripStatusLabel("X: ---  Y: ---");
            coordLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            coordLabel.Width = 260;

            zoomLabel = new ToolStripStatusLabel("Zoom: 1.0x");
            zoomLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            zoomLabel.Width = 120;

            measureLabel = new ToolStripStatusLabel("");
            measureLabel.Width = 400;

            statusStrip.Items.Add(modeLabel);
            statusStrip.Items.Add(coordLabel);
            statusStrip.Items.Add(zoomLabel);
            statusStrip.Items.Add(measureLabel);

            this.Controls.Add(statusStrip);
        }

        internal void ExportViewportToPng()
        {
            var ld = ActiveLayerDisplay;
            if (ld == null) return;
            try
            {
                using (Bitmap bmp = ld.CaptureViewport())
                {
                    if (bmp == null) return;
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "PNG Image|*.png";
                        sfd.DefaultExt = "png";
                        sfd.FileName = "gerber_view.png";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                            modeLabel.Text = "Exported: " + Path.GetFileName(sfd.FileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message);
            }
        }

        internal void UpdateStatusBar(LayerDisplay ld)
        {
            if (ld == null) return;
            if (ld.MeasureMode)
            {
                modeLabel.Text = "Measure";
                measureButton.Checked = true;
            }
            else
            {
                modeLabel.Text = "Navigate";
                measureButton.Checked = false;
            }
            coordLabel.Text = string.Format("X: {0:F3}  Y: {1:F3}", Document.MouseX, Document.MouseY);
            zoomLabel.Text = string.Format("Zoom: {0:F2}x", ld.Zoomlevel);

            if (ld.MeasureMode && ld.LastDistance > 0)
            {
                if (ld.PolylineMode)
                    measureLabel.Text = string.Format("Seg: {0:F2} mm | Total: {1:F2} mm", ld.LastDistance, ld.PolylineTotal);
                else
                    measureLabel.Text = string.Format("Dist: {0:F2} mm", ld.LastDistance);
            }
            else
            {
                measureLabel.Text = "";
            }
        }

        public void ShowDockContent()
        {

      

            TheTopDisplay = new LayerDisplay(Document, BoardSide.Top, this);
            TheTopDisplay.Show(this.dockPanel, DockState.Document);
            TheTopDisplay.Text = "Top";
            TheBottomDisplay = new LayerDisplay(Document, BoardSide.Bottom, this);
            TheBottomDisplay.Show(this.dockPanel, DockState.Document);
            TheBottomDisplay.Text = "Bottom";

            TheList = new LayerList(this, Document, _log);
            TheList.Show(this.dockPanel, DockState.DockLeft);
            TheList.Width = 480;
        }

        public void LoadGerberFolder(List<string> list)
        {
            if (list == null) return;
            try
            {
                foreach (var a in list)
                {
                    Document.AddFile(_log, a);
                    AddRecentFile(a);
                }
                UpdateAll();

                TheTopDisplay.PerformZoomToFit();
                TheBottomDisplay.PerformZoomToFit();

            ClearDisplays();

                foreach (var a in Document.Gerbers)
                {
                    a.Panel = new LayerDisplay(Document, a, this);
                    a.Panel.Show(this.dockPanel, DockState.Document);
                    a.Panel.Text = Path.GetFileName(a.File.Name);
                    SingleLayers.Add(a.Panel);
                }

                TheTopDisplay.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace +ex.Message);
            }
        }

        internal void RefreshDisplays()
        {
            if (TheTopDisplay == null) return;
            TheTopDisplay.UpdateDocument(false);
            if (TheBottomDisplay != null) TheBottomDisplay.UpdateDocument(false);
            foreach (var a in SingleLayers) a.UpdateDocument(false);
        }

        internal void MarkFileDirtyAndRefresh(ParsedGerber file)
        {
            if (TheTopDisplay == null || file == null) return;
            TheTopDisplay.MarkFileDirty(file);
            if (TheBottomDisplay != null) TheBottomDisplay.MarkFileDirty(file);
            foreach (var a in SingleLayers) a.MarkFileDirty(file);
            RefreshDisplays();
        }

        internal void UpdateAll(bool reloadlist = true)
        {
            if (reloadlist) TheList?.UpdateLoadedStuff();

            if (TheTopDisplay == null) return;
            TheTopDisplay.ClearCache(true);
            TheBottomDisplay.ClearCache(true);
            foreach (var a in SingleLayers) a.ClearCache(true);

            TheTopDisplay.UpdateDocument(reloadlist);
            TheBottomDisplay.UpdateDocument(reloadlist);
            foreach(var a in SingleLayers)
            {
                a.UpdateDocument(reloadlist);
            }
        }
        public void ClearDisplays()
        {
            foreach (var a in SingleLayers)
            {
                a.DockHandler.DockPanel = null;
                a.Close();
                
            }
            TheTopDisplay.ClearCache(true);
            TheBottomDisplay.ClearCache(true);
            SingleLayers.Clear();
        }
        internal void ClearAll()
        {
            Document.Gerbers.Clear();
            ClearDisplays();
            UpdateAll();
        }


        private void GerberViewerMainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if ((e.KeyState & 8) == 8)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.Link;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void GerberViewerMainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (Document.Gerbers.Count >0)
                {
                    //if (MessageBox.Show("Clear first?", "Clear?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    if ((e.KeyState & 8) != 8)
                    {
                        
                            ClearAll();   
                    }
                }
                string[] D = e.Data.GetData(DataFormats.FileDrop) as string[];
                List<String> files = new List<string>();
                foreach (string S in D)
                {
                    if (Directory.Exists(S))
                    {
                        LoadGerberFolder(Directory.GetFiles(S).ToList());
                    }
                    else
                    {
                        if (File.Exists(S)) files.Add(S);
                    }
                }
                if (files.Count > 0)
                {
                    LoadGerberFolder(files);
                }
            }
        }

        internal void SetMouseCoord(float x, float y)
        {
            Document.CrossHairActive = true;
            Document.MouseX = x;
            Document.MouseY = y;
            RefreshDisplays();
            UpdateStatusBar(ActiveLayerDisplay);
        }

        internal void MouseOut()
        {
            Document.CrossHairActive = false;
            RefreshDisplays();
            UpdateStatusBar(ActiveLayerDisplay);
        }

        internal void ActivateTab(int rowIndex)
        {
            Document.Gerbers[rowIndex].Panel.Activate();
        }
    }
}
