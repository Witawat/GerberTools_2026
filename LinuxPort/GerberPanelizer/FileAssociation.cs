using System;
using System.IO;

namespace GerberCombinerBuilder
{
    public static class FileAssociation
    {
        public static bool IsRegistered()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return false;
            return IsRegisteredWindows();
        }

        public static void Register()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;
            RegisterWindows();
        }

        public static void Unregister()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;
            UnregisterWindows();
        }

        private static bool IsRegisteredWindows()
        {
            using (var extKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + Extension))
            {
                if (extKey == null) return false;
                var defaultVal = extKey.GetValue(null) as string;
                if (defaultVal != ProgID) return false;
            }
            using (var progKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + ProgID + @"\shell\open\command"))
            {
                if (progKey == null) return false;
                var command = progKey.GetValue(null) as string;
                if (string.IsNullOrEmpty(command)) return false;
                if (!command.Contains(Path.GetFileName(GetExePath()))) return false;
            }
            return true;
        }

        private static void RegisterWindows()
        {
            string exePath = GetExePath();
            using (var extKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + Extension))
                extKey.SetValue(null, ProgID);
            using (var progKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgID))
                progKey.SetValue(null, Description);
            using (var iconKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgID + @"\DefaultIcon"))
                iconKey.SetValue(null, "\"" + exePath + "\",0");
            using (var cmdKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgID + @"\shell\open\command"))
                cmdKey.SetValue(null, "\"" + exePath + "\" \"%1\"");
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        private static void UnregisterWindows()
        {
            try { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + ProgID); } catch { }
            try { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + Extension); } catch { }
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        private const string Extension = ".gerberset";
        private const string ProgID = "GerberTools.gerberset";
        private const string Description = "GerberTools Panel Layout";

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        private static string GetExePath()
        {
            var path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? System.Reflection.Assembly.GetEntryAssembly()?.Location
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            return path;
        }
    }
}
