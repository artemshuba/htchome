using Home.Base;
using Home.Base.Widgets;
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

namespace HTCHome.Widgets
{
    public sealed class WidgetManager
    {
        private const string WIDGETS_DIR = "Widgets";
        private const string WIDGET_MANIFEST = "widget.json";
        private const string WIDGET_STATE = "widget_state.json";
        private string _widgetsRootPath = Path.Combine(E.Root, WIDGETS_DIR);

        private Dictionary<string, WidgetDescriptor> _catalog = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, WidgetInstance> _instances = new(StringComparer.OrdinalIgnoreCase);

        private IStateStore _stateStore;

        public IReadOnlyList<String> RunningWidgetIds => _instances.Values.Select(i => i.WidgetId).ToList();

        private WidgetManager(IStateStore stateStore)
        { 
            _stateStore = stateStore;
        }

        public static async Task<WidgetManager> CreateAsync(IStateStore stateStore)
        {
            var widgetManager = new WidgetManager(stateStore);
            await widgetManager.InitializeAsync();
            return widgetManager;
        }

        public async Task LoadWidgetsAsync(IList<string>? widgets)
        {
            if (widgets != null && widgets.Count > 0)
            {
                foreach (var widgetId in widgets)
                {
                    await LoadWidgetAsync(widgetId);
                }
            }

            // If we got running widgets, skip loading default ones
            if (_instances.Count > 0)
                return;

            var defaultWidgets = _catalog.Values.Where(w => w.Manifest.IsDefault).ToList();

            foreach (var widget in defaultWidgets)
            {
                await LoadWidgetAsync(widget.Manifest.Id);
            }
        }

        public async Task LoadWidgetAsync(string id)
        {
            var manifest = _catalog.GetValueOrDefault(id);
            if (manifest == null)
                return; // TODO: throw exception?

            var assemblyPath = Path.Combine(manifest.DirectoryPath, manifest.Manifest.AssemblyName);
            var assemblyContext = new WidgetLoadContext(assemblyPath);
            var assembly = assemblyContext.LoadFromAssemblyName(new AssemblyName(manifest.Manifest.Id));
            var widgetType = assembly.GetExportedTypes().Where(type => typeof(IWidget).IsAssignableFrom(type)).FirstOrDefault();

            if (widgetType == null)
            {
                // TODO: logging
                Debug.WriteLine($"Dll {assembly} doesn't contain widget type.");
                return; // TODO: throw exception?
            }

            var widgetStatePath = Path.Combine(manifest.DirectoryPath, WIDGET_STATE);
            var widgetState = await _stateStore.LoadAsync<WidgetState>(widgetStatePath);

            var widget = Activator.CreateInstance(widgetType) as IWidget;
            if (widget == null)
                return; // TODO: throw exception?
            var widgetView = widget.CreateView();
            var window = new WidgetWindow();

            if (widgetState != null)
            {
                window.Left = widgetState.X;
                window.Top = widgetState.Y;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
            }

            window.Content = widgetView;
            window.Closing += WidgetWindow_Closing;
            window.RemoveRequested += WidgetWindow_RemoveRequested;

            window.Show();

            var instance = new WidgetInstance()
            {
                WidgetId = id,
                InstanceId = Guid.NewGuid().ToString("N"),
                Widget = widget,
                Window = window,
                AssemblyLoadContext = assemblyContext
            };

            _instances[instance.InstanceId] = instance;
        }

        public async Task ShutdownAsync()
        {
        }

        private async Task SaveWidgetStateAsync(WidgetInstance instance)
        {
            var widgetDescriptor = _catalog[instance.WidgetId];
            if (widgetDescriptor == null)
            {
                return;
            }

            var widgetStatePath = Path.Combine(widgetDescriptor.DirectoryPath, WIDGET_STATE);

            var state = new WidgetState()
            {
                X = (float)instance.Window.Left,
                Y = (float)instance.Window.Top
            };

            await _stateStore.SaveAsync(state, widgetStatePath);
        }

        private void DeleteWidgetState(WidgetInstance instance)
        {
            var widgetDescriptor = _catalog[instance.WidgetId];
            if (widgetDescriptor == null)
            {
                return;
            }

            var widgetStatePath = Path.Combine(widgetDescriptor.DirectoryPath, WIDGET_STATE);

            _stateStore.Delete(widgetStatePath);
        }

        private async Task InitializeAsync()
        {
            _catalog = (await EnumerateWidgetsAsync()).ToDictionary(d => d.Manifest.Id, d => d, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<List<WidgetDescriptor>> EnumerateWidgetsAsync()
        {
            var result = new List<WidgetDescriptor>();
            var dirs = Directory.EnumerateDirectories(_widgetsRootPath);

            foreach (var widgetDir in dirs)
            {
                var manifestPath = Path.Combine(widgetDir, WIDGET_MANIFEST);
                if (!File.Exists(manifestPath))
                    continue; // skip if there is no manifest in the directory

                try
                {
                    var manifestJson = await File.ReadAllTextAsync(manifestPath);
                    var manifest = JsonSerializer.Deserialize<WidgetManifest>(manifestJson, options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (manifest == null)
                        continue; // skip if manifest is broken

                    if (String.IsNullOrEmpty(manifest.Id) || String.IsNullOrEmpty(manifest.DisplayName) || String.IsNullOrEmpty(manifest.AssemblyName))
                        continue; // skip if manifest is not valid

                    var assemblyPath = Path.Combine(widgetDir, manifest.AssemblyName);

                    if (!File.Exists(assemblyPath))
                        continue; // skip if assembly does not exist

                    // if everything looks fine, add to the list
                    var descriptor = new WidgetDescriptor()
                    {
                        Manifest = manifest,
                        DirectoryPath = widgetDir
                    };

                    result.Add(descriptor);
                }
                catch (Exception ex)
                {
                    // TODO: logging
                    Debug.WriteLine($"Failed to load widget manifest from {manifestPath}: {ex.Message}");
                }
            }

            return result;
        }
        private async void WidgetWindow_Closing(object? sender, CancelEventArgs e)
        {
            var widgetInstance = _instances.Values.FirstOrDefault(i => i.Window == sender);
            if (widgetInstance != null)
            {
                await SaveWidgetStateAsync(widgetInstance);
            }

            Application.Current.Shutdown();
        }

        private async void WidgetWindow_RemoveRequested(object? sender, EventArgs e)
        {
            var widgetInstance = _instances.Values.FirstOrDefault(i => i.Window == sender);
            if (widgetInstance != null)
            {
                DeleteWidgetState(widgetInstance);

                widgetInstance.Window.Closing -= WidgetWindow_Closing;
                widgetInstance.Window.RemoveRequested -= WidgetWindow_RemoveRequested;

                _instances.Remove(widgetInstance.InstanceId);
            }

            if (_instances.Count == 0)
            {
                Application.Current.Shutdown();
            }
        }
    }

    class WidgetLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public WidgetLoadContext(string path)
        {
            _resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (string.IsNullOrEmpty(assemblyPath))
                return null;

            return LoadFromAssemblyPath(assemblyPath);
        }
    }
}