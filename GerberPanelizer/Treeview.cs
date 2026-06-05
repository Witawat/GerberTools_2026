using GerberLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GerberCombinerBuilder
{
    public partial class Treeview :  Form
    {
        TreeNode Gerbers;
        TreeNode BreakTabs;
        TreeNode RootNode;
        GerberPanelize TargetHost;
        public Treeview()
        {
            InitializeComponent();

            Gerbers = new TreeNode("Gerbers");
            BreakTabs = new TreeNode("Breaktabs");

            TreeNode[] array = new TreeNode[] { Gerbers, BreakTabs };

            RootNode = new TreeNode("Board", array);
            treeView1.Nodes.Add(RootNode);
            treeView1.ExpandAll();
            treeView1.Enabled = false;
        }
        class InstanceTreeNode : TreeNode
        {
            public AngledThing TargetInstance;

            public InstanceTreeNode(AngledThing GI)
                : base("instance")
            {
                TargetInstance = GI;
                Text = ToString();
            }
            public override string ToString()
            {
                if (TargetInstance.GetType() == typeof(GerberInstance))
                {
                    var gi = TargetInstance as GerberInstance;
                    return String.Format(CultureInfo.InvariantCulture, "Instance: {0} {1:F3},{2:F3} {3:F1}", Path.GetFileNameWithoutExtension(gi.GerberPath), TargetInstance.Center.X, TargetInstance.Center.Y, TargetInstance.Angle);
                }
                else if (TargetInstance is BreakTab bt)
                {
                    return String.Format(CultureInfo.InvariantCulture, "Tab @ {0:F1},{1:F1} R:{2:F2} Err:{3}", TargetInstance.Center.X, TargetInstance.Center.Y, bt.Radius, bt.Errors.Count);
                }
                return "unknown";
            }
        }

        class GerberFileNode : TreeNode
        {
            public string pPath;
            public GerberFileNode(string path)
                : base("gerber")
            {
                pPath = path;
                Text = ToString();
            }
            public override string ToString()
            {
                return Path.GetFileNameWithoutExtension(pPath);
            }
        }

        public void BuildTree(GerberPanelize Parent,  GerberLayoutSet S)
        {
            TargetHost = Parent;
            if (TargetHost == null) { treeView1.Enabled = false; return; } else { treeView1.Enabled = true; };
            while (Gerbers.Nodes.Count > 0)
            {
                Gerbers.Nodes[0].Remove();
            }
            while (BreakTabs.Nodes.Count > 0)
            {
                BreakTabs.Nodes[0].Remove();
            }
            foreach (var a in S.LoadedOutlines)
            {
                Gerbers.Nodes.Add(new GerberFileNode(a));
            }

            foreach (var a in S.Instances)
            {
                foreach (GerberFileNode t in Gerbers.Nodes)
                {
                    if (t.pPath == a.GerberPath)
                    {
                        t.Nodes.Add(new InstanceTreeNode(a));
                    }
                }

            }

            foreach (GerberFileNode t in Gerbers.Nodes)
            {
                t.Text = Path.GetFileNameWithoutExtension(t.pPath) + " (" + t.Nodes.Count + " instances)";
            }


            foreach (var t in S.Tabs)
            {
                BreakTabs.Nodes.Add(new InstanceTreeNode(t));
            }

            treeView1.ExpandAll();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode.GetType() == typeof(InstanceTreeNode))
            {
                TargetHost.SetSelectedInstance((treeView1.SelectedNode as InstanceTreeNode).TargetInstance);
            }
        }

        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                TreeNode node = treeView1.GetNodeAt(e.Location);
                if (node == null) return;
                if (node.GetType() == typeof(InstanceTreeNode))
                {
                    var P = PointToScreen(new Point(e.X, e.Y));
                    contextMenuStrip1.Show(P);
                    treeView1.SelectedNode = node;
                }
                if (node.GetType() == typeof(GerberFileNode))
                {
                    var P = PointToScreen(new Point(e.X, e.Y));
                    contextMenuStrip1.Show(P);
                    treeView1.SelectedNode = node;
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.GetType() == typeof(InstanceTreeNode))
            {
                TargetHost.RemoveInstance((treeView1.SelectedNode as InstanceTreeNode).TargetInstance);
            }
            if (treeView1.SelectedNode.GetType() == typeof(GerberFileNode))
            {
                TargetHost.PushUndo();
                var path = (treeView1.SelectedNode as GerberFileNode).pPath;
                if (TargetHost.ThePanel.TheSet.LoadedOutlines.Contains(path))
                {
                    TargetHost.ThePanel.TheSet.LoadedOutlines.Remove(path);
                    TargetHost.ThePanel.GerberOutlines.Remove(path);
                    TargetHost.TV.BuildTree(TargetHost, TargetHost.ThePanel.TheSet);
                    TargetHost.Redraw(true);
                }
            }
        }

        private void resetRotationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.GetType() == typeof(InstanceTreeNode))
            {
                var inst = (treeView1.SelectedNode as InstanceTreeNode).TargetInstance;
                inst.Angle = 0;
                TargetHost.SetSelectedInstance(inst);
                TargetHost.Redraw(true);
            }
        }

        private void addTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (TargetHost != null)
            {
                TargetHost.AddTab(new GerberLibrary.Core.Primitives.PointD(TargetHost.ThePanel.TheSet.Width / 2, TargetHost.ThePanel.TheSet.Height / 2));
            }
        }

        private void addInstanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = "";
            if (treeView1.SelectedNode.GetType() == typeof(GerberFileNode))
            {
                path = (treeView1.SelectedNode as GerberFileNode).pPath;
            }
            if (treeView1.SelectedNode.GetType() == typeof(InstanceTreeNode))
            {
                if ((treeView1.SelectedNode as InstanceTreeNode).TargetInstance.GetType() == typeof(GerberInstance))
                {
                    path = ((treeView1.SelectedNode as InstanceTreeNode).TargetInstance as GerberInstance).GerberPath;
                }
            }
            if (path.Length > 0)
            {
                TargetHost.AddInstance(path, new GerberLibrary.Core.Primitives.PointD(0,0));
            }
        }

        private void exportBoardImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = "";
            if (treeView1.SelectedNode.GetType() == typeof(GerberFileNode))
            {
                path = (treeView1.SelectedNode as GerberFileNode).pPath;
            }
            if (treeView1.SelectedNode.GetType() == typeof(InstanceTreeNode))
            {
                if ((treeView1.SelectedNode as InstanceTreeNode).TargetInstance.GetType() == typeof(GerberInstance))
                {
                    path = ((treeView1.SelectedNode as InstanceTreeNode).TargetInstance as GerberInstance).GerberPath;
                }
            }
            if (path.Length > 0)
            {

                try
                {
                    System.Windows.Forms.SaveFileDialog OFD = new System.Windows.Forms.SaveFileDialog();

                    OFD.DefaultExt = "";
                    if (OFD.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                    Console.WriteLine("path selected: {0}", path);

                    GerberImageCreator GIC = new GerberImageCreator();

                    foreach (var a in Directory.GetFiles(path, "*.*"))
                    {
                        GIC.AddBoardToSet(a, new SilentLog());
                    }

                    GIC.WriteImageFiles(OFD.FileName, showimage: Gerber.DirectlyShowGeneratedBoardImages);

                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error exporting board image:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
