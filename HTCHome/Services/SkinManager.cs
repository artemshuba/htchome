using Home.Base.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HTCHome.Services
{
    public class SkinManager
    {
        private const string SKINS_DIR = "Skins";
        private readonly string _skinsRootPath;
        private readonly ILogger _logger;
        
        public List<string> AvailableSkins { get; private set; } = new();
        public string CurrentSkin { get; private set; } = "Default";

        public SkinManager(string rootPath, ILogger logger)
        {
            _skinsRootPath = Path.Combine(rootPath, SKINS_DIR);
            _logger = logger;
            EnsureDefaultSkin();
        }

        private void EnsureDefaultSkin()
        {
            if (!Directory.Exists(_skinsRootPath))
            {
                Directory.CreateDirectory(_skinsRootPath);
            }
            // Ideally we would extract a default skin here if missing
        }

        public void LoadAvailableSkins()
        {
            AvailableSkins.Clear();
            if (Directory.Exists(_skinsRootPath))
            {
                var dirs = Directory.GetDirectories(_skinsRootPath);
                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    // Check if skin.xaml exists
                    if (File.Exists(Path.Combine(dir, "Skin.xaml")))
                    {
                        AvailableSkins.Add(name);
                    }
                }
            }
            _logger.Info($"Found {AvailableSkins.Count} skins.");
        }

        public void ApplySkin(string skinName)
        {
            if (string.IsNullOrEmpty(skinName)) return;

            var skinPath = Path.Combine(_skinsRootPath, skinName, "Skin.xaml");
            if (!File.Exists(skinPath))
            {
                _logger.Error($"Skin {skinName} not found at {skinPath}");
                return;
            }

            try
            {
                var skinUri = new Uri(skinPath, UriKind.Absolute);
                var skinDict = new ResourceDictionary { Source = skinUri };

                // Clear old skins? 
                // Strategy: We want to replace resources. 
                // Simple approach: Add to MergedDictionaries. 
                // Better approach: Tag the skin dictionary and remove old one.

                var oldSkin = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Contains("IsSkin"));
                if (oldSkin != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldSkin);
                }

                skinDict["IsSkin"] = true; // Tag it
                Application.Current.Resources.MergedDictionaries.Add(skinDict);
                
                CurrentSkin = skinName;
                _logger.Info($"Applied skin: {skinName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply skin {skinName}", ex);
            }
        }
    }
}
