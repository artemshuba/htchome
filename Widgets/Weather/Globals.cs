using System;
using System.IO;
using System.Windows.Threading;
using System.Media;
using System.Reflection;
using Weather.Domain;
using Home.Base;

namespace Weather
{
    public static class Globals
    {
        public static Settings Settings;
        public static WeatherProviderManager WpManager;
        public static string ConfigFile;
        public static DispatcherTimer UpdateTimer;
        public static SoundPlayer SoundPlayer;
        public static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void Initialize()
        {
             if (string.IsNullOrEmpty(ConfigFile))
                ConfigFile = E.Root + "\\Weather.config";
            
            Settings = (Settings)XmlSerializable.Load(typeof(Settings), ConfigFile) ?? new Settings();
            
            WpManager = new WeatherProviderManager();
            // Provider loading logic might need to be called explicitly
        }
    }
}
