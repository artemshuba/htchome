using Home.Base;
using Home.Base.Widgets;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Media;

namespace HTCHome.Widgets
{
    public class WidgetManager
    {
        private const string WIDGETS_DIR = "Widgets";

        private string _widgetsRootPath = Path.Combine(E.Root, WIDGETS_DIR);

        public void LoadWidgets()
        {
            var dirs = Directory.GetDirectories(_widgetsRootPath);
            foreach (var widgetDir in dirs)
            {
                var widgetName = Path.GetFileName(widgetDir);
                var widgetDll = Directory.GetFiles(widgetDir, "*.dll").FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == widgetName);
                if (string.IsNullOrEmpty(widgetDll))
                    continue;

                var assemblyContext = new WidgetLoadContext(widgetDll);
                var assembly = assemblyContext.LoadFromAssemblyName(new AssemblyName(widgetName));
                var widgetType = assembly.GetExportedTypes().Where(type => typeof(IWidget).IsAssignableFrom(type)).FirstOrDefault();

                if (widgetType == null)
                {
                    Debug.WriteLine($"Dll {widgetDll} doesn't contain widget type.");
                    continue;
                }

                var widget = Activator.CreateInstance(widgetType) as IWidget;
                var widgetView = widget.GetView();
                var window = new WidgetWindow();

                window.Content = widgetView;
                window.Show();
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
}