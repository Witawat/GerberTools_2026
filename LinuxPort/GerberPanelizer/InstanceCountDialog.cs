using System;
using System.Windows.Forms;

namespace GerberCombinerBuilder
{
    public partial class InstanceCountDialog : Form
    {
        public int InstanceCount => (int)CountBox.Value;

        public InstanceCountDialog(string gerberPath)
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            PathLabel.Text = gerberPath;
            CountBox.Value = 1;
        }

        private void OkButton(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelBtnPress(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
