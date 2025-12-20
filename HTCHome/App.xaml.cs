using HTCHome.State;
using HTCHome.Utils.Helpers;
using HTCHome.Widgets;
using System.Linq;
using System.Windows;

namespace HTCHome
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private WidgetManager? _widgetManager;


        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // WidgetManager handles restore internaly now
            var widgetManager = await WidgetManager.CreateAsync();
            
            // Optionally update properties if needed, but WidgetManager is self-contained for layout
            _widgetManager = widgetManager;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_widgetManager != null)
                await _widgetManager.ShutdownAsync();

            base.OnExit(e);
        }
    }
}