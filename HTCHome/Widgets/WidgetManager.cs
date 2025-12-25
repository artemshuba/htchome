using Home.Base;
using Home.Base.Services;
using Home.Base.Widgets;
using HTCHome.Services;
using HTCHome.Utils.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HTCHome.Widgets
{
    public sealed class WidgetManager
    {
        private const string WIDGETS_DIR = "Widgets";
        private const string WIDGET_MANIFEST = "widget.json";
        private const string CONFIG_DIR = "Config";
        private const string LAYOUT_FILE = "layout.json";

        private string _widgetsRootPath = Path.Combine(E.Root, WIDGETS_DIR);
        private string _configRootPath;

        private Dictionary<string, WidgetDescriptor> _catalog = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, WidgetInstance> _instances = new(StringComparer.OrdinalIgnoreCase);
        // Cache contexts to support multiple instances of same widget without reloading assembly
        private Dictionary<string, WidgetLoadContext> _loadedContexts = new(StringComparer.OrdinalIgnoreCase);

        // Services
        private ILogger _logger;
        private ExtensionManager _extensionManager;
        private NetworkService _networkService;
        
        public IReadOnlyList<string> RunningWidgetIds => _instances.Values.Select(i => i.WidgetId).ToList();

        private WidgetManager()
        { 
            // Initialize Config Path (Try AppDir, fallback to AppData)
            _configRootPath = Path.Combine(E.Root, CONFIG_DIR);
            try
            {
                if (!Directory.Exists(_configRootPath))
                    Directory.CreateDirectory(_configRootPath);
            }
            catch (UnauthorizedAccessException)
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _configRootPath = Path.Combine(appData, "HTCHome", CONFIG_DIR);
                if (!Directory.Exists(_configRootPath))
                    Directory.CreateDirectory(_configRootPath);
            }

            // Initialize Services
            _logger = new FileLogger(Path.Combine(_configRootPath, "Logs", $"htchome.log"));
            _extensionManager = new ExtensionManager(_logger);
            _networkService = new NetworkService();
        }

        public static async Task<WidgetManager> CreateAsync()
        {
            var widgetManager = new WidgetManager();
            await widgetManager.InitializeAsync();
            return widgetManager;
        }

        private async Task InitializeAsync()
        {
            _logger.Info("Initializing WidgetManager...");
            
            // 1. Load Catalogue
            _catalog = (await EnumerateWidgetsAsync()).ToDictionary(d => d.Manifest.Id, d => d, StringComparer.OrdinalIgnoreCase);
            _logger.Info($"Loaded {_catalog.Count} widgets in catalog.");

            // 2. Load Extensions
             await _extensionManager.LoadExtensionsAsync(Path.Combine(E.Root, "Extras\\Weather"));

            // 3. Restore Layout
            await RestoreLayoutAsync();

            // 4. Load Defaults if no layout
            if (_instances.Count == 0)
            {
                var defaults = _catalog.Values.Where(w => w.Manifest.IsDefault).ToList();
                if (defaults.Count > 0)
                {
                    _logger.Info($"No layout found. Loading {defaults.Count} default widgets.");
                    foreach (var widget in defaults)
                    {
                        await LoadWidgetInstanceAsync(widget.Manifest.Id, null);
                    }
                }
                else
                {
                     _logger.Info("No layout found and no default widgets defined.");
                }
            }
        }

        public async Task LoadWidgetsAsync(IList<string>? widgets)
        {
            // This method seems to be used for initial load "suggestions" or CLI args?
            // If we have saved layout, we prefer that.
            if (_instances.Count > 0)
                return;

            if (widgets != null && widgets.Count > 0)
            {
                foreach (var widgetId in widgets)
                {
                    await LoadWidgetInstanceAsync(widgetId, null);
                }
            }
            else
            {
               // Load defaults
                var defaultWidgets = _catalog.Values.Where(w => w.Manifest.IsDefault).ToList();
                foreach (var widget in defaultWidgets)
                {
                    await LoadWidgetInstanceAsync(widget.Manifest.Id, null);
                }
            }
        }
        
        public async Task LoadWidgetAsync(string id)
        {
            await LoadWidgetInstanceAsync(id, null);
        }

        private async Task LoadWidgetInstanceAsync(string widgetId, WidgetLayoutItem? layoutItem)
        {
            _logger.Info($"Loading widget {widgetId}...");

            if (!_catalog.TryGetValue(widgetId, out var descriptor))
            {
                _logger.Error($"Widget {widgetId} not found in catalog.");
                return;
            }

            WidgetLoadContext? loadContext = null;
            IWidget? widget = null;
            WidgetWindow? window = null;

            try
            {
                var assemblyPath = Path.Combine(descriptor.DirectoryPath, descriptor.Manifest.AssemblyName);
                
                // Check cache first
                if (_loadedContexts.TryGetValue(widgetId, out var existingContext))
                {
                    loadContext = existingContext;
                    _logger.Info($"Reusing existing context for {widgetId}");
                }
                else
                {
                    loadContext = new WidgetLoadContext(assemblyPath);
                    _loadedContexts[widgetId] = loadContext;
                }
                
                var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(descriptor.Manifest.Id));
                var widgetType = assembly.GetExportedTypes().FirstOrDefault(type => typeof(IWidget).IsAssignableFrom(type));

                if (widgetType == null)
                {
                    _logger.Error($"Assembly {assemblyPath} does not contain an IWidget implementation.");
                    loadContext.Unload();
                    return;
                }

                widget = Activator.CreateInstance(widgetType) as IWidget;
                if (widget == null)
                {
                    _logger.Error($"Failed to instantiate widget {widgetType.Name}");
                    loadContext.Unload();
                    return;
                }

                // Prepare Context
                string instanceId = layoutItem?.InstanceId ?? Guid.NewGuid().ToString("N");
                string configPath = Path.Combine(_configRootPath, "Widgets", instanceId + ".json");
                
                var configService = new JsonConfigurationService(configPath);
                await configService.LoadAsync();

                var widgetContext = new WidgetContext(
                    instanceId, 
                    widgetId,
                    descriptor.DirectoryPath,
                    _logger,
                    configService,
                    _networkService,
                    _extensionManager);

                // Initialize Widget
                widget.Initialize(widgetContext);

                // Create View
                var view = widget.CreateView();
                
                // Create Window
                window = new WidgetWindow();
                window.Content = view;
                window.Resources.MergedDictionaries.Add(widgetContext.SkinResources);

                // Position
                if (layoutItem != null)
                {
                    window.Left = layoutItem.X;
                    window.Top = layoutItem.Y;
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                window.Closing += WidgetWindow_Closing;
                window.RemoveRequested += WidgetWindow_RemoveRequested;
                window.ExitRequested += (s, e) => Application.Current.Shutdown();
                window.GlobalSettingsRequested += (s, e) =>
                {
                    try
                    {
                        var settingsView = new GlobalSettingsControl();
                        var settingsWindow = new SettingsWindow("HTC Home Settings", settingsView, null);
                        settingsWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to open Global Settings", ex);
                        MessageBox.Show($"Error opening settings: {ex.Message}", "HTC Home", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                window.SettingsRequested += (s, e) => 
                {
                    try
                    {
                        // Get custom settings view if available
                        var settingsView = (widget as IConfigurableWidget)?.CreateSettingsView();

                        // Always open settings window (it now has General/Skins tab)
                        var settingsWindow = new SettingsWindow($"Settings - {descriptor.Manifest.DisplayName}", settingsView, widgetContext.SkinService);
                        settingsWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to open settings for {widgetId}", ex);
                        MessageBox.Show($"Error opening settings: {ex.Message}", "HTC Home", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };



                // Populate Add Widget Menu
                if (window.ContextMenu.Items[0] is MenuItem addWidgetMenu) 
                {
                    foreach(var kvp in _catalog)
                    {
                        var item = new MenuItem { Header = kvp.Value.Manifest.DisplayName };
                        item.Click += async (s, e) => await LoadWidgetInstanceAsync(kvp.Key, null);
                        addWidgetMenu.Items.Add(item);
                    }
                }

                window.Show();

                var instance = new WidgetInstance()
                {
                    InstanceId = instanceId,
                    WidgetId = widgetId,
                    Widget = widget,
                    Window = window,
                    AssemblyLoadContext = loadContext
                };

                _instances[instanceId] = instance;
                _logger.Info($"Widget {widgetId} ({instanceId}) loaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load widget {widgetId}", ex);
                // Cleanup
                // window?.Close(); // Window might be phantom
                if (loadContext != null && !_loadedContexts.ContainsKey(widgetId))
                {
                    try { loadContext.Unload(); } catch { }
                }
            }
        }

        public async Task ShutdownAsync()
        {
            await SaveLayoutAsync();
            foreach(var instance in _instances.Values)
            {
                try
                {
                    instance.Widget.Unload();
                    instance.Window.Close();
                }
                catch(Exception ex)
                {
                    _logger.Error($"Error unloading instance {instance.InstanceId}", ex);
                }
            }
            _instances.Clear();
        }

        private async Task SaveLayoutAsync()
        {
            var layout = new List<WidgetLayoutItem>();
            foreach (var instance in _instances.Values)
            {
                layout.Add(new WidgetLayoutItem
                {
                    WidgetId = instance.WidgetId,
                    InstanceId = instance.InstanceId,
                    X = instance.Window.Left,
                    Y = instance.Window.Top
                });
            }

            try
            {
                var layoutPath = Path.Combine(_configRootPath, LAYOUT_FILE);
                var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(layoutPath, json);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save layout", ex);
            }
        }

        private async Task RestoreLayoutAsync()
        {
            var layoutPath = Path.Combine(_configRootPath, LAYOUT_FILE);
            if (!File.Exists(layoutPath)) return;

            try
            {
                var json = await File.ReadAllTextAsync(layoutPath);
                var layout = JsonSerializer.Deserialize<List<WidgetLayoutItem>>(json);
                if (layout != null)
                {
                    foreach (var item in layout)
                    {
                        await LoadWidgetInstanceAsync(item.WidgetId, item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to restore layout", ex);
            }
        }

        private async Task<List<WidgetDescriptor>> EnumerateWidgetsAsync()
        {
            var result = new List<WidgetDescriptor>();
            if (!Directory.Exists(_widgetsRootPath))
            {
                 Directory.CreateDirectory(_widgetsRootPath);
                 return result;
            }

            var dirs = Directory.EnumerateDirectories(_widgetsRootPath);

            foreach (var widgetDir in dirs)
            {
                var manifestPath = Path.Combine(widgetDir, WIDGET_MANIFEST);
                if (!File.Exists(manifestPath)) continue;

                try
                {
                    var manifestJson = await File.ReadAllTextAsync(manifestPath);
                    var manifest = JsonSerializer.Deserialize<WidgetManifest>(manifestJson, options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (manifest != null && !string.IsNullOrEmpty(manifest.Id))
                    {
                        result.Add(new WidgetDescriptor
                        {
                            Manifest = manifest,
                            DirectoryPath = widgetDir
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load manifest from {manifestPath}", ex);
                }
            }

            return result;
        }

        private async void WidgetWindow_Closing(object? sender, CancelEventArgs e)
        {
             // This event fires when Close() is called or Alt+F4
             // If generic Close, we probably want to Exit app if it's the last window?
             // Or just Save layout?
             // Actually, if we just close window, we are not removing the widget.
             // But if we Exit App, windows are closed.
             // Let's defer "App Shutdown" logic to App.xaml.cs, here we just track instances.
        }

        private async void WidgetWindow_RemoveRequested(object? sender, EventArgs e)
        {
            var widgetInstance = _instances.Values.FirstOrDefault(i => i.Window == sender);
            if (widgetInstance != null)
            {
                // Remove
                _instances.Remove(widgetInstance.InstanceId);
                
                try 
                {
                    widgetInstance.Widget.Unload();
                }
                catch(Exception ex) 
                {
                    _logger.Error("Error unloading widget", ex);
                }

                // Unsubscribe events to avoid leaks
                widgetInstance.Window.Closing -= WidgetWindow_Closing;
                widgetInstance.Window.RemoveRequested -= WidgetWindow_RemoveRequested;

                // Close Window
                widgetInstance.Window.Close();

                // Delete state?
                 // var configPath = Path.Combine(_configRootPath, "Widgets", widgetInstance.InstanceId + ".json");
                 // File.Delete(configPath); 
                 // Maybe keep state if user wants to undo? Or stricter cleanup? 
                 // User said "Widget unloading should be done...". Usually implies full removal.
            }

            // Unload context if last instance
            if (!_instances.Values.Any(i => i.WidgetId == widgetInstance?.WidgetId))
            {
                if (widgetInstance != null && _loadedContexts.TryGetValue(widgetInstance.WidgetId, out var ctx))
                {
                    _logger.Info($"Unloading context for {widgetInstance.WidgetId} as last instance removed.");
                    try { ctx.Unload(); } catch { }
                    _loadedContexts.Remove(widgetInstance.WidgetId);
                }
            }

            if (_instances.Count == 0)
            {
                 Application.Current.Shutdown();
            }
            else
            {
                 await SaveLayoutAsync();
            }
        }
        public void ToggleWidgetsVisibility()
        {
            foreach (var instance in _instances.Values)
            {
                if (instance.Window.Visibility == Visibility.Visible)
                {
                    instance.Window.Hide();
                }
                else
                {
                    instance.Window.Show();
                    instance.Window.Activate();
                }
            }
        }
    }

    public class WidgetLayoutItem
    {
        public string InstanceId { get; set; } = "";
        public string WidgetId { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
    }

    class WidgetLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public WidgetLoadContext(string path) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Delegate shared assemblies to Default Context to ensure type identity
            if (assemblyName.Name == "Home.Base" || 
                assemblyName.Name == "Weather.Base" ||
                assemblyName.Name == "NLog" || 
                assemblyName.Name == "Newtonsoft.Json")
            {
                return null;
            }

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (string.IsNullOrEmpty(assemblyPath))
                return null;

            return LoadFromAssemblyPath(assemblyPath);
        }
    }
}
