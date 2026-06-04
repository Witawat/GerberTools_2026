using System;
using System.Windows.Forms;

namespace GerberCombinerBuilder
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += (sender, e) =>
            {
                Logger.Log(e.Exception, "UI Thread Exception");
                MessageBox.Show("Unhandled UI thread exception:\n" + e.Exception + "\n\nLogged to: " + Logger.LogPath,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Logger.Log(ex ?? new Exception(e.ExceptionObject?.ToString() ?? "null"), "Unhandled Exception");
                MessageBox.Show("Unhandled exception:\n" + (ex?.Message ?? e.ExceptionObject?.ToString()) + "\n\nLogged to: " + Logger.LogPath,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GerberPanelizerParent());
        }
    }
}
