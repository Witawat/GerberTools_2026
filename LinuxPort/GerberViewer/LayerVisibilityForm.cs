using GerberLibrary;
using GerberLibrary.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GerberViewer
{
    public partial class LayerVisibilityForm : Form
    {
        private LayerDisplay _display;
        private LoadedStuff _doc;
        private CheckedListBox checkedListBox;
        private TrackBar alphaTrackBar;
        private Label alphaLabel;
        private Label alphaValueLabel;
        private Button btnSelectAll;
        private Button btnDeselectAll;
        private Button btnInvert;
        private Button btnOK;
        private Button btnCancel;
        private int selectedIndex = -1;

        public LayerVisibilityForm(LayerDisplay display, LoadedStuff doc)
        {
            _display = display;
            _doc = doc;
            Text = "Layer Visibility — " + (display.DisplaySide == BoardSide.Top ? "Top" : "Bottom");
            Size = new Size(400, 540);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            BuildUI();
            PopulateLayers();
        }

        private void BuildUI()
        {
            checkedListBox = new CheckedListBox();
            checkedListBox.Location = new Point(10, 10);
            checkedListBox.Size = new Size(364, 320);
            checkedListBox.CheckOnClick = true;
            checkedListBox.IntegralHeight = false;
            checkedListBox.SelectedIndexChanged += (s, e) =>
            {
                selectedIndex = checkedListBox.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < _doc.Gerbers.Count)
                {
                    alphaTrackBar.Value = (int)(_doc.Gerbers[selectedIndex].Alpha * 100);
                    UpdateAlphaLabel();
                }
            };

            alphaLabel = new Label();
            alphaLabel.Text = "Opacity:";
            alphaLabel.Location = new Point(10, 340);
            alphaLabel.Size = new Size(55, 20);

            alphaTrackBar = new TrackBar();
            alphaTrackBar.Location = new Point(65, 338);
            alphaTrackBar.Size = new Size(220, 40);
            alphaTrackBar.Minimum = 10;
            alphaTrackBar.Maximum = 100;
            alphaTrackBar.TickFrequency = 10;
            alphaTrackBar.Value = 100;
            alphaTrackBar.Scroll += (s, e) =>
            {
                if (selectedIndex >= 0 && selectedIndex < _doc.Gerbers.Count)
                {
                    _doc.Gerbers[selectedIndex].Alpha = alphaTrackBar.Value / 100.0f;
                    UpdateAlphaLabel();
                }
            };

            alphaValueLabel = new Label();
            alphaValueLabel.Text = "100%";
            alphaValueLabel.Location = new Point(290, 340);
            alphaValueLabel.Size = new Size(40, 20);

            var buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 80;

            btnSelectAll = new Button();
            btnSelectAll.Text = "All On";
            btnSelectAll.Size = new Size(75, 22);
            btnSelectAll.Location = new Point(10, 10);
            btnSelectAll.Click += (s, e) =>
            {
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                    checkedListBox.SetItemChecked(i, true);
            };

            btnDeselectAll = new Button();
            btnDeselectAll.Text = "All Off";
            btnDeselectAll.Size = new Size(75, 22);
            btnDeselectAll.Location = new Point(90, 10);
            btnDeselectAll.Click += (s, e) =>
            {
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                    checkedListBox.SetItemChecked(i, false);
            };

            btnInvert = new Button();
            btnInvert.Text = "Invert";
            btnInvert.Size = new Size(75, 22);
            btnInvert.Location = new Point(170, 10);
            btnInvert.Click += (s, e) =>
            {
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                    checkedListBox.SetItemChecked(i, !checkedListBox.GetItemChecked(i));
            };

            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Size = new Size(75, 22);
            btnOK.Location = new Point(10, 45);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += (s, e) => ApplyChanges();

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(75, 22);
            btnCancel.Location = new Point(90, 45);
            btnCancel.DialogResult = DialogResult.Cancel;

            buttonPanel.Controls.Add(btnSelectAll);
            buttonPanel.Controls.Add(btnDeselectAll);
            buttonPanel.Controls.Add(btnInvert);
            buttonPanel.Controls.Add(btnOK);
            buttonPanel.Controls.Add(btnCancel);

            Controls.Add(checkedListBox);
            Controls.Add(alphaLabel);
            Controls.Add(alphaTrackBar);
            Controls.Add(alphaValueLabel);
            Controls.Add(buttonPanel);
        }

        private void UpdateAlphaLabel()
        {
            alphaValueLabel.Text = string.Format("{0}%", alphaTrackBar.Value);
        }

        private void PopulateLayers()
        {
            for (int i = 0; i < _doc.Gerbers.Count; i++)
            {
                var g = _doc.Gerbers[i];
                string label = string.Format("{0} [{1}] \u03B1={2}% {3}",
                    g.File.Layer, g.File.Side, (int)(g.Alpha * 100),
                    System.IO.Path.GetFileName(g.File.Name));
                bool isChecked = !_display.HiddenLayerIndices.Contains(i);
                checkedListBox.Items.Add(label, isChecked);
            }
            if (checkedListBox.Items.Count > 0)
            {
                checkedListBox.SelectedIndex = 0;
            }
        }

        private void ApplyChanges()
        {
            _display.HiddenLayerIndices.Clear();
            for (int i = 0; i < _doc.Gerbers.Count; i++)
            {
                if (i < checkedListBox.Items.Count && !checkedListBox.GetItemChecked(i))
                    _display.HiddenLayerIndices.Add(i);
            }
        }
    }
}
