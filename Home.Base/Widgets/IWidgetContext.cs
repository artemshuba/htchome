using System;
using System.Collections.Generic;
using Home.Base.Services;

namespace Home.Base.Widgets
{
    public interface IWidgetContext
    {
        string InstanceId { get; }
        string WidgetId { get; }
        
        ILogger Logger { get; }
        IConfigurationService Configuration { get; }
        INetworkService Network { get; }

        /// <summary>
        /// Gets the skin service for managing local widget skins.
        /// </summary>
        ISkinService SkinService { get; }

        /// <summary>
        /// Gets a list of available extensions of type T.
        /// </summary>
        IEnumerable<T> GetExtensions<T>() where T : IExtension;
        
        /// <summary>
        /// Gets an absolute URI for a widget asset.
        /// Use this for Image Sources etc.
        /// </summary>
        Uri GetAssetUri(string relativePath);

        /// <summary>
        /// Gets the physical path for a widget asset.
        /// Use this only if you need direct file access (avoid if possible).
        /// </summary>
        string GetAssetPath(string relativePath);
    }
}
