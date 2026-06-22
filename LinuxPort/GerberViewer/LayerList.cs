using GerberLibrary;
using GerberLibrary.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GerberViewer
{
    public partial class LayerList : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        LoadedStuff Document;
        GerberViewerMainForm ParentGerberViewerForm;
        
        private Panel buttonPanel = new Panel();
        private DataGridView dataGridView1 = new DataGridView();
        private Button addNewRowButton = new Button();
        private Button deleteRowButton = new Button();
        private Button clearButton = new Button();
        private Button save2ImgButton = new Button();
        private ProgressLog _log;

        public LayerList(GerberViewerMainForm parent,  LoadedStuff doc,ProgressLog log)
        {

            ParentGerberViewerForm = parent;
            Document = doc;
            CloseButton = false;
            CloseButtonVisible = false;

            InitializeComponent();
          
            CloseButton = false;
            CloseButtonVisible = false;
            _log = log;

            Init();
            UpdateLoadedStuff();
        }

        private void SetupLayout()
        {
            int col1 = 10, col2 = 95, col3 = 180, col4 = 265;
            int row1 = 8, row2 = 36;

            var showAllButton = new Button();
            showAllButton.Text = "All On";
            showAllButton.Location = new Point(col1, row1);
            showAllButton.Size = new Size(80, 22);
            showAllButton.Click += (s, ev) =>
            {
                for (int i = 0; i < Document.Gerbers.Count; i++)
                {
                    Document.Gerbers[i].visible = true;
                    if (i < dataGridView1.Rows.Count)
                        dataGridView1[0, i].Value = "\u2713";
                }
                dataGridView1.Refresh();
                ParentGerberViewerForm.RefreshDisplays();
            };

            var hideAllButton = new Button();
            hideAllButton.Text = "All Off";
            hideAllButton.Location = new Point(col2, row1);
            hideAllButton.Size = new Size(80, 22);
            hideAllButton.Click += (s, ev) =>
            {
                for (int i = 0; i < Document.Gerbers.Count; i++)
                {
                    Document.Gerbers[i].visible = false;
                    if (i < dataGridView1.Rows.Count)
                        dataGridView1[0, i].Value = "";
                }
                dataGridView1.Refresh();
                ParentGerberViewerForm.RefreshDisplays();
            };

            var invertButton = new Button();
            invertButton.Text = "Invert";
            invertButton.Location = new Point(col3, row1);
            invertButton.Size = new Size(80, 22);
            invertButton.Click += (s, ev) =>
            {
                for (int i = 0; i < Document.Gerbers.Count; i++)
                {
                    Document.Gerbers[i].visible = !Document.Gerbers[i].visible;
                    if (i < dataGridView1.Rows.Count)
                        dataGridView1[0, i].Value = Document.Gerbers[i].visible ? "\u2713" : "";
                }
                dataGridView1.Refresh();
                ParentGerberViewerForm.RefreshDisplays();
            };

            clearButton.Text = "Clear All";
            clearButton.Location = new Point(col1, row2);
            clearButton.Size = new Size(80, 22);
            clearButton.Click += new EventHandler(ClearAllButtonClick);

            save2ImgButton.Text = "Save to PNG";
            save2ImgButton.Location = new Point(col2, row2);
            save2ImgButton.Size = new Size(85, 22);
            save2ImgButton.Click += new EventHandler(SaveSelectedFile2Img);

            addNewRowButton.Text = "Add";
            addNewRowButton.Location = new Point(col3, row2);
            addNewRowButton.Size = new Size(80, 22);
            addNewRowButton.Click += new EventHandler(AddGerberFile);

            deleteRowButton.Text = "Remove";
            deleteRowButton.Location = new Point(col4, row2);
            deleteRowButton.Size = new Size(80, 22);
            deleteRowButton.Click += new EventHandler(RemoveGerberFile);

            buttonPanel.Controls.Add(showAllButton);
            buttonPanel.Controls.Add(hideAllButton);
            buttonPanel.Controls.Add(invertButton);
            buttonPanel.Controls.Add(clearButton);
            buttonPanel.Controls.Add(save2ImgButton);
            buttonPanel.Controls.Add(addNewRowButton);
            buttonPanel.Controls.Add(deleteRowButton);
            buttonPanel.Height = 68;
            buttonPanel.Dock = DockStyle.Bottom;

            this.Controls.Add(this.buttonPanel);
        }

        private void SaveSelectedFile2Img(object sender, EventArgs e)
        {
            if (_curRowIndex >= 0)
            {
                string filepath = Document.Gerbers[_curRowIndex].File.Name;
                string savepath = CreateImageForSingleFile(filepath, Color.Black, Color.White, _log);
                MessageBox.Show("Image saved: " + savepath);
                return;
            }
            MessageBox.Show("Please choose a file first !");
        }

        private string CreateImageForSingleFile(string arg, Color Foreground, Color Background,ProgressLog log)
        {
            string savePath = string.Empty;
            int dpi = 720;
            if (arg.ToLower().EndsWith(".png") == true) return null;
            GerberImageCreator.AA = false;
            //Gerber.Verbose = true;
            if (Gerber.ThrowExceptions)
            {
                Gerber.SaveGerberFileToImageUnsafe(log, arg, arg + "_render.png", dpi, Foreground, Background);
                savePath = arg + "_render.png";
            }
            else
            {
                Gerber.SaveGerberFileToImage(log, arg, arg + "_render.png", dpi, Foreground, Background);
                savePath = arg + "_render.png";
            }

            if (Gerber.SaveDebugImageOutput)
            {
                Gerber.SaveDebugImage(arg, arg + "_debugviz.png", dpi, Foreground, Background, log);
                savePath = arg + "_debugviz.png";
            }
            return savePath;
        }
        private void RemoveGerberFile(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            MessageBox.Show("Sorry, this feature has not been implemented yet.");
        }

        private void AddGerberFile(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            MessageBox.Show("Sorry, this feature has not been implemented yet."+Environment.NewLine
                + "You can directly drag files to the list area on the left to add files.");
        }

        public void Init()
        {
            SetupLayout();
            SetupDataGridView();
        }
        private void SetupDataGridView()
        {
            this.Controls.Add(dataGridView1);

            dataGridView1.ColumnCount = 6;

            dataGridView1.Columns[0].Name = "Vis";
            dataGridView1.Columns[0].Width = 30;
            dataGridView1.Columns[1].Name = "Colour";
            dataGridView1.Columns[1].Width = 40;
            dataGridView1.Columns[2].Name = "File";
            dataGridView1.Columns[2].Width = 140;
            dataGridView1.Columns[3].Name = "Layer";
            dataGridView1.Columns[3].Width = 80;
            dataGridView1.Columns[4].Name = "Side";
            dataGridView1.Columns[4].Width = 55;
            dataGridView1.Columns[5].Name = "Alpha";
            dataGridView1.Columns[5].Width = 45;

            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(64, 64, 68);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font =
                new Font(dataGridView1.Font, FontStyle.Bold);

            dataGridView1.BackgroundColor = Color.FromArgb(37, 37, 38);
            dataGridView1.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
            dataGridView1.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 220);
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

            dataGridView1.Name = "songsDataGridView";
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dataGridView1.GridColor = Color.FromArgb(60, 60, 62);
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.ReadOnly = true;
            dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView1.CellClick += DataGridView1_CellClick;
            dataGridView1.CellPainting += DataGridView1_CellPainting;
            dataGridView1.RowPrePaint += DataGridView1_RowPrePaint;
        }

        private int _curRowIndex = -1;
        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.RowIndex >= Document.Gerbers.Count) return;

            if (e.ColumnIndex == 0)
            {
                var gerb = Document.Gerbers[e.RowIndex];
                gerb.visible = !gerb.visible;
                dataGridView1[0, e.RowIndex].Value = gerb.visible ? "\u2713" : "";
                dataGridView1.InvalidateCell(e.ColumnIndex, e.RowIndex);
                ParentGerberViewerForm.RefreshDisplays();
                return;
            }

            if (dataGridView1.Columns[e.ColumnIndex].Name == "Colour")
            {
                var gerb = Document.Gerbers[e.RowIndex];
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = gerb.Color;
                    cd.FullOpen = true;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        gerb.Color = cd.Color;
                        dataGridView1.InvalidateRow(e.RowIndex);
                        ParentGerberViewerForm.MarkFileDirtyAndRefresh(gerb.File);
                    }
                }
                return;
            }

            if (dataGridView1.Columns[e.ColumnIndex].Name == "Alpha")
            {
                var gerb = Document.Gerbers[e.RowIndex];
                float[] alphas = { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };
                int curIdx = 0;
                for (int i = 0; i < alphas.Length; i++)
                {
                    if (Math.Abs(gerb.Alpha - alphas[i]) < 0.05f) { curIdx = i; break; }
                }
                gerb.Alpha = alphas[(curIdx + 1) % alphas.Length];
                dataGridView1[5, e.RowIndex].Value = string.Format("{0}%", (int)(gerb.Alpha * 100));
                ParentGerberViewerForm.MarkFileDirtyAndRefresh(gerb.File);
                return;
            }

            ParentGerberViewerForm.ActivateTab(e.RowIndex);
            _curRowIndex = e.RowIndex;
        }

        private void DataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var colourCol = dataGridView1.Columns["Colour"];
            if (colourCol != null && e.ColumnIndex == colourCol.Index && e.RowIndex >= 0)
            {
                if (e.Value != null)
                {
                    e.PaintBackground(e.ClipBounds, true);
                    var C = Color.FromArgb(int.Parse((string)e.Value));
                    using (Brush backColorBrush = new SolidBrush(C))
                    {
                        Rectangle r = new Rectangle(e.CellBounds.X + 4,
                            e.CellBounds.Y + 4, e.CellBounds.Width - 8,
                            e.CellBounds.Height - 8);
                        e.Graphics.FillRectangle(backColorBrush, r);
                        e.Graphics.DrawRectangle(Pens.Gray, r);
                    }
                    e.Handled = true;
                }
            }

            var visCol = dataGridView1.Columns["Vis"];
            if (visCol != null && e.ColumnIndex == visCol.Index && e.RowIndex >= 0)
            {
                e.PaintBackground(e.ClipBounds, true);
                if (e.RowIndex < Document.Gerbers.Count && Document.Gerbers[e.RowIndex].visible)
                {
                    TextRenderer.DrawText(e.Graphics, "\u2713",
                        dataGridView1.DefaultCellStyle.Font ?? this.Font,
                        e.CellBounds, Color.Lime,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                e.Handled = true;
            }
        }

        private void DataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= Document.Gerbers.Count) return;
            var side = Document.Gerbers[e.RowIndex].File.Side;
            Color rowColor;
            switch (side)
            {
                case GerberLibrary.Core.BoardSide.Top:
                    rowColor = Color.FromArgb(55, 20, 20);
                    break;
                case GerberLibrary.Core.BoardSide.Bottom:
                    rowColor = Color.FromArgb(20, 20, 55);
                    break;
                case GerberLibrary.Core.BoardSide.Both:
                    rowColor = Color.FromArgb(20, 50, 20);
                    break;
                default:
                    rowColor = Color.FromArgb(50, 50, 30);
                    break;
            }
            dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = rowColor;
        }


        public void UpdateLoadedStuff()
        {
            dataGridView1.Rows.Clear();
            foreach(var a in Document.Gerbers)
            {
                List<string> V = new List<string>();
                V.Add(a.visible ? "\u2713" : "");
                V.Add(a.Color.ToArgb().ToString());
                V.Add(Path.GetFileName(a.File.Name));
                V.Add(a.File.Layer.ToString());
                V.Add(a.File.Side.ToString());
                V.Add(string.Format("{0}%", (int)(a.Alpha * 100)));

                dataGridView1.Rows.Add(V.ToArray());
            };
        }

        private void LayerList_DragEnter(object sender, DragEventArgs e)
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

        private void LayerList_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {

                string[] D = e.Data.GetData(DataFormats.FileDrop) as string[];
                List<String> files = new List<string>();
                foreach (string S in D)
                {
                    if (Directory.Exists(S))
                    {
                        ParentGerberViewerForm.LoadGerberFolder(Directory.GetFiles(S).ToList());
                    }
                    else
                    {
                        if (File.Exists(S)) files.Add(S);
                    }
                }
                if (files.Count > 0)
                {
                    ParentGerberViewerForm.LoadGerberFolder(files);
                }
            }
        }

        private void ClearAllButtonClick(object sender, EventArgs e)
        {
            ParentGerberViewerForm.ClearAll();
        }
    }
}
