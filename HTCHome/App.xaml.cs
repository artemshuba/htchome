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
        private Services.TrayIconService? _trayIconService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // WidgetManager handles restore internaly now
            var widgetManager = await WidgetManager.CreateAsync();
            _widgetManager = widgetManager;

            // Initialize Tray Icon
            _trayIconService = new Services.TrayIconService(widgetManager);
            _trayIconService.Initialize();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _trayIconService?.Dispose();

            if (_widgetManager != null)
                await _widgetManager.ShutdownAsync();

            base.OnExit(e);
        }
    }
}