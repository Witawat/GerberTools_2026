using GerberLibrary;
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

namespace GerberCombinerBuilder
{
    public partial class InstanceDialog :Form
    {
        GerberPanelize TargetInstance;
        public InstanceDialog()
        {
            InitializeComponent();
            panel1.Enabled = false;
            System.Windows.Forms.ToolTip toolTip = new System.Windows.Forms.ToolTip();
            toolTip.SetToolTip(Up, "Move up (Shift+Y)");
            toolTip.SetToolTip(Down, "Move down (Shift+N)");
            toolTip.SetToolTip(LeftButton, "Move left");
            toolTip.SetToolTip(RightButton, "Move right");
            toolTip.SetToolTip(Clock, "Rotate +90°");
            toolTip.SetToolTip(AClock, "Rotate -90°");
            toolTip.SetToolTip(Bigger, "Increase breaktab radius");
            toolTip.SetToolTip(Smaller, "Decrease breaktab radius");
            toolTip.SetToolTip(ZoomInButton, "Zoom in");
            toolTip.SetToolTip(ZoomOutButton, "Zoom out");
        }
        private bool Initializing = true;
        private double LastAngle;

        public void UpdateBoxes(GerberPanelize newTarget)
        {
            TargetInstance = newTarget;

            if (TargetInstance == null) { panel1.Enabled = false; return; } else { panel1.Enabled = true; }
            newTarget.SuspendRedraw = true;

            try
            {

            
            bool hasSelection = TargetInstance.SelectedInstance != null;
            tableLayoutPanel1.Enabled = hasSelection;
            Up.Enabled = hasSelection;
            Down.Enabled = hasSelection;
            LeftButton.Enabled = hasSelection;
            RightButton.Enabled = hasSelection;
            Clock.Enabled = hasSelection;
            AClock.Enabled = hasSelection;

            
            if (!hasSelection)
            {
                return;
            }

            double x = TargetInstance.SelectedInstance.Center.X;
            double y = TargetInstance.SelectedInstance.Center.Y;
            double r = TargetInstance.SelectedInstance.Angle;
            double rad = 0;

            radiusbox.Enabled = false;
            Bigger.Enabled = false;
            Smaller.Enabled = false;

            if (TargetInstance.SelectedInstance.GetType() == typeof(BreakTab))
            {
                BreakTab BT = TargetInstance.SelectedInstance as BreakTab;
                NameLabel.Text = "Break tab";
                Bigger.Enabled = true;
                Smaller.Enabled = true;
                rad = BT.Radius;
                radiusbox.Enabled = true;
            }
           
           if (TargetInstance.SelectedInstance.GetType() == typeof(GerberInstance))
            {
                  GerberInstance GI = TargetInstance.SelectedInstance as GerberInstance;
                  NameLabel.Text = Path.GetFileName(Path.GetDirectoryName( GI.GerberPath));
            }

            Initializing = true;
            xbox.Value = (decimal)x;
            ybox.Value = (decimal)y;
            rbox.Value = (decimal)r;
            radiusbox.Value = (decimal)rad;
            LastAngle = r;
            Initializing = false;
            }
            finally
            {
                newTarget.SuspendRedraw = false;
            }

        }

        void UpdateInstance()
        {
            if (Initializing) return;

            if (TargetInstance == null) return;
            
            if (TargetInstance.SelectedInstance == null) return;

            double newAngle = (double)rbox.Value;

            if (TargetInstance.SelectedInstance.GetType() == typeof(BreakTab))
            {
                TargetInstance.SelectedInstance.Center.X = (double)xbox.Value;
                TargetInstance.SelectedInstance.Center.Y = (double)ybox.Value;
                TargetInstance.SelectedInstance.Angle = newAngle;
                BreakTab BT = TargetInstance.SelectedInstance as BreakTab;
                BT.Radius = (double)radiusbox.Value;
            }
            else if (TargetInstance.SelectedInstance.GetType() == typeof(GerberInstance))
            {
                bool angleChanged = (Math.Abs(newAngle - LastAngle) > 0.001);
                bool posChanged = (Math.Abs((double)xbox.Value - TargetInstance.SelectedInstance.Center.X) > 0.001)
                               || (Math.Abs((double)ybox.Value - TargetInstance.SelectedInstance.Center.Y) > 0.001);

                if (angleChanged && !posChanged)
                {
                    TargetInstance.RotateInstanceTo(TargetInstance.SelectedInstance, newAngle);
                }
                else if (angleChanged && posChanged)
                {
                    TargetInstance.SelectedInstance.Center.X = (double)xbox.Value;
                    TargetInstance.SelectedInstance.Center.Y = (double)ybox.Value;
                    TargetInstance.RotateInstanceTo(TargetInstance.SelectedInstance, newAngle);
                    TargetInstance.SelectedInstance.Center.X = (double)xbox.Value;
                    TargetInstance.SelectedInstance.Center.Y = (double)ybox.Value;
                    GerberInstance GI = TargetInstance.SelectedInstance as GerberInstance;
                    GI.RebuildTransformed(TargetInstance.ThePanel.GerberOutlines[GI.GerberPath], TargetInstance.ThePanel.TheSet.ExtraTabDrillDistance);
                }
                else
                {
                    TargetInstance.SelectedInstance.Center.X = (double)xbox.Value;
                    TargetInstance.SelectedInstance.Center.Y = (double)ybox.Value;
                    GerberInstance GI = TargetInstance.SelectedInstance as GerberInstance;
                    GI.RebuildTransformed(TargetInstance.ThePanel.GerberOutlines[GI.GerberPath], TargetInstance.ThePanel.TheSet.ExtraTabDrillDistance);
                }
            }

            LastAngle = newAngle;
            Initializing = true;
            if (TargetInstance.SelectedInstance != null)
            {
                xbox.Value = (decimal)TargetInstance.SelectedInstance.Center.X;
                ybox.Value = (decimal)TargetInstance.SelectedInstance.Center.Y;
            }
            Initializing = false;

            TargetInstance.UpdateHoverControls();
            TargetInstance.Redraw(true);
            TargetInstance.TV.BuildTree(TargetInstance, TargetInstance.ThePanel.TheSet);
        }

        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            if (TargetInstance != null) TargetInstance.ZoomIn();
        }

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            if (TargetInstance != null) TargetInstance.ZoomOut();
        }

        private void Up_Click(object sender, EventArgs e)
        {
            ybox.Value += 1;
            UpdateInstance();
        }

        private void Left_Click(object sender, EventArgs e)
        {
            xbox.Value -= 1;
            UpdateInstance();
        }

        private void Right_Click(object sender, EventArgs e)
        {
            xbox.Value += 1;
            UpdateInstance();
        }

        private void Down_Click(object sender, EventArgs e)
        {
            ybox.Value -= 1;
            UpdateInstance();
        }

        private void AClock_Click(object sender, EventArgs e)
        {
            decimal newval = rbox.Value - 90;
            if (newval < -180) newval += 360;
            rbox.Value = newval;
            UpdateInstance();
        }

        private void Clock_Click(object sender, EventArgs e)
        {
            decimal newval = rbox.Value + 90;
            if (newval > 180) newval -= 360;
            rbox.Value = newval;
            UpdateInstance();
        }

        private void xbox_ValueChanged(object sender, EventArgs e)
        {
            UpdateInstance();
        }

        private void ybox_ValueChanged(object sender, EventArgs e)
        {
            UpdateInstance();
       
        }

        private void rbox_ValueChanged(object sender, EventArgs e)
        {
            UpdateInstance();
       
        }

        private void Bigger_Click(object sender, EventArgs e)
        {
            radiusbox.Value += 1;
            UpdateInstance();
      
        }

        private void Smaller_Click(object sender, EventArgs e)
        {
            radiusbox.Value -= 1;
            
            UpdateInstance();
      
        }

        private void radiusbox_ValueChanged(object sender, EventArgs e)
        {
            UpdateInstance();
     
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }

    }
}
