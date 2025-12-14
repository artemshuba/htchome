using Home.Base;
using Home.Base.Widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace HTCHome.Widgets
{
    public sealed class WidgetManager
    {
        private const string WIDGETS_DIR = "Widgets";
        private const string WIDGET_MANIFEST = "widget.json";
        private string _widgetsRootPath = Path.Combine(E.Root, WIDGETS_DIR);

        private List<WidgetDescriptor> _widgets = new List<WidgetDescriptor>();

        private WidgetManager()
        { 
        }

        public static async Task<WidgetManager> CreateAsync()
        {
            var widgetManager = new WidgetManager();
            await widgetManager.InitializeAsync();
            return widgetManager;
        }

        public void LoadWidgetAsync(string id)
        {
            var manifest = _widgets.FirstOrDefault(w => w.Manifest.Id == id);
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

            var widget = Activator.CreateInstance(widgetType) as IWidget;
            var widgetView = widget.CreateView();
            var window = new WidgetWindow();

            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.Content = widgetView;

            window.Show();
        }

        private async Task InitializeAsync()
        {
            _widgets = await EnumerateWidgetsAsync();
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
    }

    class WidgetLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public WidgetLoadContext(string path)
        {
            _resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
}