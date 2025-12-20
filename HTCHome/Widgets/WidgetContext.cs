using Home.Base.Services;
using Home.Base.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace HTCHome.Widgets
{
    public class WidgetContext : IWidgetContext
    {
        public string InstanceId { get; }
        public string WidgetId { get; }
        public ILogger Logger { get; }
        public IConfigurationService Configuration { get; }
        public INetworkService Network { get; }
        
        private readonly string _widgetDirectory;
        private readonly IExtensionManager _extensionManager; // We need to define this

        public IEnumerable<T> GetExtensions<T>() where T : IExtension
        {
            return _extensionManager.GetExtensions<T>();
        }
        
        public ISkinService SkinService { get; }

        public WidgetContext(
            string instanceId, 
            string widgetId, 
            string widgetDirectory,
            ILogger logger, 
            IConfigurationService configuration, 
            INetworkService network,
            IExtensionManager extensionManager)
        {
            InstanceId = instanceId;
            WidgetId = widgetId;
            _widgetDirectory = widgetDirectory;
            Logger = logger;
            Configuration = configuration;
            Network = network;
            _extensionManager = extensionManager;
            SkinService = new WidgetSkinService(_widgetDirectory, logger);
        }

        private class WidgetSkinService : ISkinService
        {
            private readonly string _widgetDir;
            private readonly ILogger _logger;

            public WidgetSkinService(string widgetDir, ILogger logger)
            {
                _widgetDir = widgetDir;
                _logger = logger;
                LoadSkins();
            }

            public IEnumerable<string> AvailableSkins { get; private set; } = new List<string>();
            public string CurrentSkin { get; private set; } = "Default";

            private void LoadSkins()
            {
                var skinsDir = Path.Combine(_widgetDir, "Skins");
                if (Directory.Exists(skinsDir))
                {
                    var skins = new List<string>();
                    foreach (var dir in Directory.GetDirectories(skinsDir))
                    {
                        if (File.Exists(Path.Combine(dir, "Skin.xaml")))
                        {
                            skins.Add(Path.GetFileName(dir));
                        }
                    }
                    AvailableSkins = skins;
                }
            }

            public void ApplySkin(string skinName)
            {
                var skinPath = Path.Combine(_widgetDir, "Skins", skinName, "Skin.xaml");
                if (File.Exists(skinPath))
                {
                    try
                    {
                        var skinUri = new Uri(skinPath, UriKind.Absolute);
                        var skinDict = new ResourceDictionary { Source = skinUri };
                        
                        // Tagging strategy: Remove old widget skin, add new one.
                        // Since we are in the widget context, we should ideally target the widget's resources.
                        // But accessing the view instance is hard here.
                        // Fallback: Add to Application Resources but with a key?
                        // No, UserControl should look for it.
                        
                        // For this iteration, we will just update CurrentSkin property.
                        // The Widget Settings UI (ViewModel) will use this to update persistence.
                        // Actual visual update might require reload or advanced binding in the View.
                        CurrentSkin = skinName;
                        
                        // Apply to App Resources effectively applying to ALL widgets if not careful.
                        // BUT, if we clear old one?
                        // "Skins must be stored inside widget... related to the widget itself".
                        // This implies the widget should handle the "Applying".
                        // We will allow the widget to Pull the resource dictionary if needed.
                        
                        // For the purpose of "Testing the skin system", we'll just try to apply to App Resources for now, 
                        // acknowledging this affects global style which is a limitation of this quick refactor.
                        // To do it properly per-instance, the WidgetView needs to merge it into its own Resources.
                        
                         var oldSkin = System.Windows.Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Contains("IsWidgetSkin"));
                        if (oldSkin != null)
                        {
                            System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldSkin);
                        }

                        skinDict["IsWidgetSkin"] = true; 
                        System.Windows.Application.Current.Resources.MergedDictionaries.Add(skinDict);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to load skin {skinName}", ex);
                    }
                }
            }
        }

        public Uri GetAssetUri(string relativePath)
        {
            // We can return a specific Pack URI or file URI.
            // For now, let's return absolute file URI.
            var path = GetAssetPath(relativePath);
            return new Uri(path);
        }

        public string GetAssetPath(string relativePath)
        {
            return Path.Combine(_widgetDirectory, relativePath);
        }
    }


}
