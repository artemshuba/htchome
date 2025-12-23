using Home.Base.Services;
using Home.Base.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HTCHome.Widgets
{
    public class ExtensionManager : IExtensionManager
    {
        private readonly List<IExtension> _extensions = new();
        private readonly ILogger _logger;

        public ExtensionManager(ILogger logger)
        {
            _logger = logger;
        }

        public async Task LoadExtensionsAsync(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                _logger.Warning($"Extensions directory not found: {rootPath}");
                return;
            }

            _logger.Info($"Scanning for extensions in {rootPath}...");
            
            // Assume 2 levels deep? Extras/Weather/Provider.dll or just flat?
            // User source has Extensions/Weather/AccuWeather.
            // Build output to Extras/Weather/AccuWeather.dll.
            // So recursively find DLLs? 
            // Or strictly: root/Category/Name/Name.dll? 
            // Let's do recursive *.dll search but be careful about deps.
            // Ideally we only load the main assembly.
            // But how do we know which one?
            // Scan all, check for IExtension.
            
            var dlls = Directory.GetFiles(rootPath, "*.dll", SearchOption.AllDirectories);
            foreach (var dllPath in dlls)
            {
                if (Path.GetFileName(dllPath).Equals("Home.Base.dll", StringComparison.OrdinalIgnoreCase)) continue;
                if (Path.GetFileName(dllPath).Equals("Weather.Base.dll", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    // Basic loading. For complex deps, use LoadContext.
                    // Since these are shared extensions, we load into default/current context via LoadFrom.
                    var assembly = Assembly.LoadFrom(dllPath);
                    
                    var extensionTypes = assembly.GetExportedTypes()
                        .Where(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in extensionTypes)
                    {
                        try
                        {
                            var extension = Activator.CreateInstance(type) as IExtension;
                            if (extension != null)
                            {
                                _extensions.Add(extension);
                                _logger.Info($"Loaded extension: {type.Name} from {Path.GetFileName(dllPath)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Failed to instantiate extension {type.Name}", ex);
                        }
                    }
                }
                catch (BadImageFormatException) { } // Not a managed assembly
                catch (Exception ex)
                {
                    // _logger.Debug($"Skipping {Path.GetFileName(dllPath)}: {ex.Message}");
                }
            }
            
            await Task.CompletedTask;
        }

        public void RegisterExtension(IExtension extension)
        {
            _extensions.Add(extension);
        }

        public IEnumerable<T> GetExtensions<T>() where T : IExtension
        {
            return _extensions.OfType<T>();
        }
    }
}
