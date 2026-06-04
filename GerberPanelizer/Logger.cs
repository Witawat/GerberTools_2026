using System;
using System.IO;

namespace GerberCombinerBuilder
{
    public static class Logger
    {
        private static readonly object _lock = new object();

        public static string LogPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath,
                        string.Format("{0:yyyy-MM-dd HH:mm:ss} {1}\n", DateTime.Now, message));
                }
            }
            catch { }
        }

        public static void Log(Exception ex, string context = "")
        {
            string ctx = string.IsNullOrEmpty(context) ? "" : "[" + context + "] ";
            Log(string.Format("{0}ERROR: {1}: {2}\n{3}", ctx, ex.GetType().Name, ex.Message, ex.StackTrace));
        }
    }
}
