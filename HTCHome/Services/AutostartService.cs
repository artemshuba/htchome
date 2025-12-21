using Microsoft.Win32;
using System;
using System.Reflection;

namespace HTCHome.Services
{
    public interface IAutostartService
    {
        bool IsAutostartEnabled { get; set; }
    }

    public class AutostartService : IAutostartService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "HTC Home";

        public bool IsAutostartEnabled
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(AppName) != null;
            }
            set
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
                if (key == null) return;

                if (value)
                {
                    // Use executable path
                    // .NET Core/5+ DLL vs Exe.Process
                    // Assembly.GetEntryAssembly().Location might return .dll
                    // Environment.ProcessPath is better for .NET 6+, but let's check Framework.
                    // For .NET 5+ single file or regular, Process.GetCurrentProcess().MainModule.FileName is reliable
                    // Or Environment.ProcessPath
                    string path = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    key.SetValue(AppName, path);
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
    }
}
