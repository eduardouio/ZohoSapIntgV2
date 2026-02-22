using System;
using System.IO;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal static class FileLogger
    {
        private static readonly object Sync = new object();
        private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ZhohoSapIntg", "logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");

        public static string CurrentLogFilePath
        {
            get { return LogFilePath; }
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                var line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}", DateTime.Now, level, message ?? string.Empty);

                lock (Sync)
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                    if (exception != null)
                    {
                        File.AppendAllText(LogFilePath, exception + Environment.NewLine);
                    }
                }
            }
            catch
            {
            }
        }
    }
}