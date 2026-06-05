using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GerberCombinerBuilder
{
    public static class FileAssociation
    {
        private const string Extension = ".gerberset";
        private const string ProgID = "GerberTools.gerberset";
        private const string Description = "GerberTools Panel Layout";

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        public static bool IsRegistered()
        {
            using (var extKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + Extension))
            {
                if (extKey == null) return false;
                var defaultVal = extKey.GetValue(null) as string;
                if (defaultVal != ProgID) return false;
            }

            using (var progKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + ProgID + @"\shell\open\command"))
            {
                if (progKey == null) return false;
                var command = progKey.GetValue(null) as string;
                if (string.IsNullOrEmpty(command)) return false;
                if (!command.Contains(Path.GetFileName(GetExePath()))) return false;
            }

            return true;
        }

        public static void Register()
        {
            string exePath = GetExePath();

            using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + Extension))
            {
                extKey.SetValue(null, ProgID);
            }

            using (var progKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgID))
            {
                progKey.SetValue(null, Description);
            }

            using (var iconKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgID + @"\DefaultIcon"))
            {
                iconKey.SetValue(null, "\"" + exePath + "\",0");
            }

            using (var cmdKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgID + @"\shell\open\command"))
            {
                cmdKey.SetValue(null, "\"" + exePath + "\" \"%1\"");
            }

            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        public static void Unregister()
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + ProgID); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + Extension); } catch { }

            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        private static string GetExePath()
        {
            // Use ProcessPath when running as published single-file, fallback to Assembly.Location
            var path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? System.Reflection.Assembly.GetEntryAssembly()?.Location
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            return path;
        }
    }
}
