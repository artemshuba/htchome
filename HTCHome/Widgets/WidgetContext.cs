using Home.Base.Services;
using Home.Base.Widgets;
using System;
using System.Collections.Generic;
using System.IO;

namespace HTCHome.Widgets
{
    public partial class WidgetContext : IWidgetContext
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
            SkinService = new WidgetSkinService(_widgetDirectory, logger, configuration);
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
