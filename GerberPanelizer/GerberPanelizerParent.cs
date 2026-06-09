using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GerberCombinerBuilder
{
    public partial class GerberPanelizerParent : Form
    {
        public Treeview TV;
        public InstanceDialog ID;
        public GerberPanelize ActivePanelizeInstance = null;

        private ToolStripMenuItem undoItem;
        private ToolStripMenuItem redoItem;


        public class ControlWriter : TextWriter
        {
            public string T = "";
            public int idx;
            bool sink = true;
            public ControlWriter()
            {

            }

            public override void Write(char value)
            {
                if (sink) return;
                idx++;
                T += value;

            }

            public override void Write(string value)
            {
                if (sink) return;
                idx++;
                T += value;
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }
        ControlWriter CW = new ControlWriter();

        public GerberPanelizerParent(string[] args = null)
        {
            InitializeComponent();
            try
            {
                var settings = Properties.Settings.Default;
                if (settings.WindowWidth > 200) this.Width = settings.WindowWidth;
                if (settings.WindowHeight > 200) this.Height = settings.WindowHeight;
            }
            catch (Exception ex) { Logger.Log(ex, "Load window size"); }
            this.FormClosing += GerberPanelizerParent_FormClosing;
            Console.SetOut(CW);
            TV = new Treeview();
            //TV.MdiParent = this;
            TV.Show();
            TV.TopLevel = false;
            TV.Dock = DockStyle.Fill;
            panel3.Controls.Add(TV);
            ID = new InstanceDialog();
            ID.Dock = DockStyle.Fill;
            ID.Show();
            ID.TopLevel = false;
            panel4.Controls.Add(ID);
            //ID.Dock = DockStyle.Left;
            //TV.Dock = DockStyle.Right;
            TV.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            ID.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

            panel3.AutoSize = false;
            this.Shown += (s, e) => { panel3.Height = panel2.Height / 2; };
            panel2.SizeChanged += (s, e) => { panel3.Height = panel2.Height / 2; };

            RemovePanelizer();

            
            //   Explanation_And_Warning EAW = new Explanation_And_Warning();
            //   EAW.Show();
            //   EAW.TopMost = true;

            //RegistryKey key = Registry.LocalMachine.OpenSubKey("CurrentUser", true);

            //key.CreateSubKey("NotRocketSciencePanelizer");
            //key = key.OpenSubKey("NotRocketSciencePanelizer", true);


            //key.CreateSubKey("0.1");
            //key = key.OpenSubKey("0.1", true);
            //int tr = (int)key.GetValue("TimesRun", (int)0);
            //key.SetValue("TimesRun", (int)(timesrun+1), RegistryValueKind.DWord);

            //if (timesrun == 0)
            //{
            //    string curDir = Directory.GetCurrentDirectory();
            //    string Url = String.Format("file:///{0}/Help/welcome.html", curDir);
            //    Process.Start(Url);
            //}
          
            
            
            // Add Edit Menu programmatically
            ToolStripMenuItem editMenu = new ToolStripMenuItem("&Edit");
            
            ToolStripMenuItem undoItem = new ToolStripMenuItem("&Undo", null, UndoToolStripMenuItem_Click);
            undoItem.ShortcutKeys = Keys.Control | Keys.Z;
            this.undoItem = undoItem;
            
            ToolStripMenuItem redoItem = new ToolStripMenuItem("&Redo", null, RedoToolStripMenuItem_Click);
            redoItem.ShortcutKeys = Keys.Control | Keys.Y;
            this.redoItem = redoItem;

            ToolStripMenuItem cutItem = new ToolStripMenuItem("Cu&t", null, CutToolStripMenuItem_Click);
            cutItem.ShortcutKeys = Keys.Control | Keys.X;

            ToolStripMenuItem copyItem = new ToolStripMenuItem("&Copy", null, CopyToolStripMenuItem_Click);
            copyItem.ShortcutKeys = Keys.Control | Keys.C;

            ToolStripMenuItem pasteItem = new ToolStripMenuItem("&Paste", null, PasteToolStripMenuItem_Click);
            pasteItem.ShortcutKeys = Keys.Control | Keys.V;

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("&Delete", null, DeleteToolStripMenuItem_Click);
            deleteItem.ShortcutKeys = Keys.Delete;

            ToolStripMenuItem duplicateItem = new ToolStripMenuItem("D&uplicate", null, (s, e) => ActivePanelizeInstance?.DuplicateSelection());
            duplicateItem.ShortcutKeys = Keys.Control | Keys.D;

            editMenu.DropDownItems.Add(undoItem);
            editMenu.DropDownItems.Add(redoItem);
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(cutItem);
            editMenu.DropDownItems.Add(copyItem);
            editMenu.DropDownItems.Add(pasteItem);
            editMenu.DropDownItems.Add(deleteItem);
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(duplicateItem);

            menuStrip.Items.Insert(1, editMenu);

            ToolStripMenuItem arrangeMenu = new ToolStripMenuItem("A&rrange");
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Align Left", null, (s, e) => ActivePanelizeInstance?.AlignLeft()));
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Align Right", null, (s, e) => ActivePanelizeInstance?.AlignRight()));
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Align Top", null, (s, e) => ActivePanelizeInstance?.AlignTop()));
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Align Bottom", null, (s, e) => ActivePanelizeInstance?.AlignBottom()));
            arrangeMenu.DropDownItems.Add(new ToolStripSeparator());
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Center Horizontally", null, (s, e) => ActivePanelizeInstance?.CenterHorizontally()));
            arrangeMenu.DropDownItems.Add(new ToolStripMenuItem("Center Vertically", null, (s, e) => ActivePanelizeInstance?.CenterVertically()));
            menuStrip.Items.Insert(2, arrangeMenu);

            ToolStripMenuItem boardMenu = new ToolStripMenuItem("&Board Placement");
            ToolStripMenuItem addGerberItem = new ToolStripMenuItem("Add Gerber Folder", null, (s, e) => ActivePanelizeInstance?.addGerberFolderToolStripMenuItem1_Click(s, e));
            ToolStripMenuItem naivePackItem = new ToolStripMenuItem("Autopack: Naive", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.RectanglePack(); ActivePanelizeInstance.Redraw(true); } });
            ToolStripMenuItem maxRectsItem = new ToolStripMenuItem("Autopack: MaxRects", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.MaxRectPack(allowrotation: true); ActivePanelizeInstance.Redraw(true); } });
            ToolStripMenuItem autofitItem = new ToolStripMenuItem("Autofit Canvas", null, (s, e) => { if (ActivePanelizeInstance != null) new AutofitDialog(ActivePanelizeInstance).ShowDialog(this); });
            boardMenu.DropDownItems.Add(addGerberItem);
            boardMenu.DropDownItems.Add(naivePackItem);
            boardMenu.DropDownItems.Add(maxRectsItem);
            boardMenu.DropDownItems.Add(new ToolStripSeparator());
            boardMenu.DropDownItems.Add(autofitItem);

            ToolStripMenuItem breaktabsMenu = new ToolStripMenuItem("&Breaktabs");
            ToolStripMenuItem insertTabItem = new ToolStripMenuItem("Insert Breaktab", null, (s, e) => ActivePanelizeInstance?.AddTab(new GerberLibrary.Core.Primitives.PointD(0, 0)));
            ToolStripMenuItem createTabsItem = new ToolStripMenuItem("Create Breaktabs", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.BuildAutoTabs(new GerberLibrary.StandardConsoleLog()); ActivePanelizeInstance.Redraw(true); ActivePanelizeInstance.TV.BuildTree(ActivePanelizeInstance, ActivePanelizeInstance.ThePanel.TheSet); } });
            ToolStripMenuItem deleteAllTabsItem = new ToolStripMenuItem("Delete all Breaktabs", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.RemoveAllTabs(false); ActivePanelizeInstance.Redraw(true); ActivePanelizeInstance.TV.BuildTree(ActivePanelizeInstance, ActivePanelizeInstance.ThePanel.TheSet); } });
            ToolStripMenuItem deleteErrorTabsItem = new ToolStripMenuItem("Delete all Breaktabs with errors", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.RemoveAllTabs(true); ActivePanelizeInstance.Redraw(true); ActivePanelizeInstance.TV.BuildTree(ActivePanelizeInstance, ActivePanelizeInstance.ThePanel.TheSet); } });
            ToolStripMenuItem mergeTabsItem = new ToolStripMenuItem("Merge Overlapping Breaktabs", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.MergeOverlappingTabs(); ActivePanelizeInstance.Redraw(true); ActivePanelizeInstance.TV.BuildTree(ActivePanelizeInstance, ActivePanelizeInstance.ThePanel.TheSet); } });
            ToolStripMenuItem doItAllItem = new ToolStripMenuItem("Do It All (remove→create→clean→merge)", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.RemoveAllTabs(); ActivePanelizeInstance.ThePanel.GenerateTabLocations(); ActivePanelizeInstance.ThePanel.RemoveAllTabs(true); ActivePanelizeInstance.ThePanel.MergeOverlappingTabs(); ActivePanelizeInstance.Redraw(true); ActivePanelizeInstance.TV.BuildTree(ActivePanelizeInstance, ActivePanelizeInstance.ThePanel.TheSet); } });
            breaktabsMenu.DropDownItems.Add(insertTabItem);
            breaktabsMenu.DropDownItems.Add(createTabsItem);
            breaktabsMenu.DropDownItems.Add(new ToolStripSeparator());
            breaktabsMenu.DropDownItems.Add(deleteAllTabsItem);
            breaktabsMenu.DropDownItems.Add(deleteErrorTabsItem);
            breaktabsMenu.DropDownItems.Add(mergeTabsItem);
            breaktabsMenu.DropDownItems.Add(new ToolStripSeparator());
            breaktabsMenu.DropDownItems.Add(doItAllItem);

            ToolStripMenuItem panelMenu = new ToolStripMenuItem("&Panel Properties");
            panelMenu.Click += (s, e) => { if (ActivePanelizeInstance != null) { new PanelProperties(ActivePanelizeInstance.ThePanel).ShowDialog(this); ActivePanelizeInstance.Redraw(true); } };

            ToolStripMenuItem viewMenu = new ToolStripMenuItem("&View");
            ToolStripMenuItem zoomFitItem = new ToolStripMenuItem("Zoom to fit", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ZoomToFit(); ActivePanelizeInstance.Redraw(false); } });
            ToolStripMenuItem scale11Item = new ToolStripMenuItem("Scale 1:1", null, (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.Zoom1to1(); ActivePanelizeInstance.Redraw(false); } });
            ToolStripMenuItem zoomInItem = new ToolStripMenuItem("Zoom In", null, (s, e) => ActivePanelizeInstance?.ZoomIn());
            ToolStripMenuItem zoomOutItem = new ToolStripMenuItem("Zoom Out", null, (s, e) => ActivePanelizeInstance?.ZoomOut());
            ToolStripMenuItem showGridItem = new ToolStripMenuItem("Show Grid", null, (s, e) => {
                if (ActivePanelizeInstance != null)
                {
                    ActivePanelizeInstance.ShowGrid = !ActivePanelizeInstance.ShowGrid;
                    ((ToolStripMenuItem)s).Checked = ActivePanelizeInstance.ShowGrid;
                    ActivePanelizeInstance.Redraw(false);
                }
            });
            showGridItem.CheckOnClick = true;
            showGridItem.Checked = true;
            viewMenu.DropDownItems.Add(zoomFitItem);
            viewMenu.DropDownItems.Add(scale11Item);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add(zoomInItem);
            viewMenu.DropDownItems.Add(zoomOutItem);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add(showGridItem);

            ToolStripMenuItem processMenu = new ToolStripMenuItem("&Process");
            processMenu.ShortcutKeys = Keys.F5;
            processMenu.Click += (s, e) => { if (ActivePanelizeInstance != null) { ActivePanelizeInstance.ThePanel.UpdateShape(new GerberLibrary.StandardConsoleLog()); ActivePanelizeInstance.Redraw(false); } };

            ToolStripMenuItem autoProcessMenu = new ToolStripMenuItem("&AutoProcess");
            autoProcessMenu.ShortcutKeys = Keys.F6;
            autoProcessMenu.CheckOnClick = true;
            autoProcessMenu.Checked = true;
            autoProcessMenu.BackColor = System.Drawing.Color.Gold;
            autoProcessMenu.Click += (s, e) => {
                autoProcessMenu.BackColor = autoProcessMenu.Checked ? System.Drawing.Color.Gold : System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Control);
                if (ActivePanelizeInstance != null)
                {
                    ActivePanelizeInstance.AutoUpdate = autoProcessMenu.Checked;
                    if (autoProcessMenu.Checked)
                    {
                        ActivePanelizeInstance.ThePanel.UpdateShape(new GerberLibrary.StandardConsoleLog());
                        ActivePanelizeInstance.Redraw(false);
                    }
                }
            };

            menuStrip.Items.Insert(3, boardMenu);
            menuStrip.Items.Insert(4, breaktabsMenu);
            menuStrip.Items.Insert(5, panelMenu);
            menuStrip.Items.Insert(6, viewMenu);
            menuStrip.Items.Insert(7, processMenu);
            menuStrip.Items.Insert(8, autoProcessMenu);

            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("&Tools");
            ToolStripMenuItem registerAssocItem = new ToolStripMenuItem("Register .gerberset File Association", null, (s, e) =>
            {
                try
                {
                    FileAssociation.Register();
                    MessageBox.Show("Registered .gerberset file association successfully.", "File Association", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to register file association:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            ToolStripMenuItem unregisterAssocItem = new ToolStripMenuItem("Unregister .gerberset File Association", null, (s, e) =>
            {
                try
                {
                    FileAssociation.Unregister();
                    MessageBox.Show("Unregistered .gerberset file association.", "File Association", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to unregister file association:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            toolsMenu.DropDownItems.Add(registerAssocItem);
            toolsMenu.DropDownItems.Add(unregisterAssocItem);
            menuStrip.Items.Add(toolsMenu);

            BuildRecentFilesMenu();
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("View Error Log", null, (s, e) =>
            {
                if (File.Exists(Logger.LogPath))
                    Process.Start(Logger.LogPath);
                else
                    MessageBox.Show("No error log found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));

            if (args != null && args.Length > 0)
            {
                var file = args[0];
                this.Shown += (s, ev) =>
                {
                    if (File.Exists(file) && Path.GetExtension(file).ToLower() == ".gerberset")
                    {
                        GerberPanelize childForm = new GerberPanelize(this, TV, ID);
                        childForm.MdiParent = this;
                        childForm.Show();
                        childForm.LoadFile(file);
                        AddRecentFile(file);
                        ActivePanelizeInstance = childForm;
                    }
                };
            }
        }
        public static int timesrun = 0;

        private void ShowNewForm(object sender, EventArgs e)
        {
            GerberPanelize childForm = new GerberPanelize(this, TV, ID);
            childForm.MdiParent = this;
            childForm.Show();
            ActivePanelizeInstance = childForm;
        }

        private void OpenFile(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFileDialog.Filter = "Gerber Set Files (*.gerberset)|*.gerberset|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
                GerberPanelize childForm = new GerberPanelize(this, TV, ID);
                childForm.MdiParent = this;
                childForm.Show();
                childForm.LoadFile(FileName);
                AddRecentFile(FileName);
                ActivePanelizeInstance = childForm;                
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActivePanelizeInstance == null) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = !string.IsNullOrEmpty(ActivePanelizeInstance.LoadedFile)
                ? Path.GetDirectoryName(ActivePanelizeInstance.LoadedFile)
                : Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveFileDialog.Filter = "Gerber Set Files (*.gerberset)|*.gerberset|All Files (*.*)|*.*";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;
                ActivePanelizeInstance.SaveFile(FileName);
                AddRecentFile(FileName);
            }
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActivePanelizeInstance != null) ActivePanelizeInstance.CutSelection();
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActivePanelizeInstance != null) ActivePanelizeInstance.CopySelection();
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActivePanelizeInstance != null) ActivePanelizeInstance.PasteSelection();
        }
        
        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
             if (ActivePanelizeInstance != null) ActivePanelizeInstance.DeleteSelection();
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
             if (ActivePanelizeInstance != null) ActivePanelizeInstance.PerformUndo();
             UpdateUndoRedoLabels();
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
             if (ActivePanelizeInstance != null) ActivePanelizeInstance.PerformRedo();
             UpdateUndoRedoLabels();
        }

        private void UpdateUndoRedoLabels()
        {
            if (ActivePanelizeInstance != null)
            {
                undoItem.Text = ActivePanelizeInstance.UndoStack.Count > 0
                    ? string.Format("&Undo ({0})", ActivePanelizeInstance.UndoStack.Count)
                    : "&Undo";
                redoItem.Text = ActivePanelizeInstance.RedoStack.Count > 0
                    ? string.Format("&Redo ({0})", ActivePanelizeInstance.RedoStack.Count)
                    : "&Redo";
            }
            else
            {
                undoItem.Text = "&Undo";
                redoItem.Text = "&Redo";
            }
        }

        

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        public void ActivatePanelizer(GerberPanelize act)
        {
            ActivePanelizeInstance = act;
            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            exportMergedGerbersToolStripMenuItem.Enabled = true;
            UpdateUndoRedoLabels();
        }


        internal void RemovePanelizer()
        {
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            exportMergedGerbersToolStripMenuItem.Enabled = false;
       
            ActivePanelizeInstance = null;
            TV.BuildTree(null, null);
            ID.UpdateBoxes(null);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 AB = new AboutBox1();
            AB.ShowDialog(this);
        }

        private void exportMergedGerbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActivePanelizeInstance != null)
            {
                ActivePanelizeInstance.exportAllGerbersToolStripMenuItem_Click(null, null);
            }
        }

        private void GerberPanelizerParent_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }

        }

        private void GerberPanelizerParent_DragDrop(object sender, DragEventArgs e)
        {
             if (e.Data.GetDataPresent(DataFormats.FileDrop))
             {
                 GerberPanelize childForm = new GerberPanelize(this, TV, ID);
                 childForm.MdiParent = this;
                 childForm.Show();
                 childForm.glControl1_DragDrop(sender, e);
                 ActivePanelizeInstance = childForm;
                 childForm.ThePanel.MaxRectPack();
                    childForm.ThePanel.BuildAutoTabs(new GerberLibrary.StandardConsoleLog());
                 childForm.Redraw(true);
             }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActivePanelizeInstance != null)
            {
                if (ActivePanelizeInstance.LoadedFile == "")
                {
                    SaveAsToolStripMenuItem_Click(sender, e);
                }
                else
                {
                    ActivePanelizeInstance.SaveFile(ActivePanelizeInstance.LoadedFile);
                }
            }
        }

        private void GerberPanelizerParent_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                var s = Properties.Settings.Default;
                s.WindowWidth = this.Width;
                s.WindowHeight = this.Height;
                if (ActivePanelizeInstance != null)
                {
                    s.LastFolder = ActivePanelizeInstance.ThePanel.TheSet.LastExportFolder;
                    s.SnapMode = (int)ActivePanelizeInstance.CurrentSnapMode;
                }
                s.Save();
            }
            catch (Exception ex) { Logger.Log(ex, "Save settings on close"); }
        }

        public void AddRecentFile(string path)
        {
            var recent = Properties.Settings.Default.RecentFiles;
            if (recent == null) recent = new System.Collections.Specialized.StringCollection();
            recent.Remove(path);
            while (recent.Count >= 10) recent.RemoveAt(recent.Count - 1);
            recent.Insert(0, path);
            Properties.Settings.Default.RecentFiles = recent;
            try { Properties.Settings.Default.Save(); } catch (Exception ex) { Logger.Log(ex, "Save recent files"); }
            BuildRecentFilesMenu();
        }

        private void BuildRecentFilesMenu()
        {
            var recentMenu = fileMenu.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(x => x.Text == "Recent Files");
            if (recentMenu == null)
            {
                recentMenu = new ToolStripMenuItem("Recent Files");
                fileMenu.DropDownItems.Insert(2, recentMenu);
            }
            recentMenu.DropDownItems.Clear();
            var recent = Properties.Settings.Default.RecentFiles;
            if (recent != null && recent.Count > 0)
            {
                foreach (string file in recent)
                {
                    if (File.Exists(file))
                    {
                        var item = new ToolStripMenuItem(Path.GetFileName(file));
                        item.ToolTipText = file;
                        item.Click += (s, e) =>
                        {
                            GerberPanelize childForm = new GerberPanelize(this, TV, ID);
                            childForm.MdiParent = this;
                            childForm.Show();
                            childForm.LoadFile(file);
                            ActivePanelizeInstance = childForm;
                        };
                        recentMenu.DropDownItems.Add(item);
                    }
                }
            }
        }
    }
}
