using Home.Base.Services;
using System;
using System.IO;

namespace HTCHome.Services
{
    public class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly object _lock = new object();

        public FileLogger(string logPath)
        {
            _logPath = logPath;
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public void Debug(string message) => Write("DEBUG", message);
        public void Info(string message) => Write("INFO", message);
        public void Warning(string message) => Write("WARN", message);
        public void Error(string message, Exception? exception = null)
        {
            var msg = message;
            if (exception != null)
            {
                msg += $"\n{exception}";
            }
            Write("ERROR", msg);
        }

        private void Write(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}");
                }
                catch
                {
                    // Should we ignore logging errors?
                }
            }
        }
    }
}
