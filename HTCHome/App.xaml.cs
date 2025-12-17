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
        private const string APP_STATE = "app_state.json";

        private WidgetManager? _widgetManager;
        private JsonStateFileStore _stateStore = new JsonStateFileStore();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var appState = await _stateStore.LoadAsync<AppState>(APP_STATE);

            var widgetManager = await WidgetManager.CreateAsync(_stateStore);

            await widgetManager.LoadWidgetsAsync(appState?.Widgets);

            _widgetManager = widgetManager;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_widgetManager != null)
                await _widgetManager.ShutdownAsync();

            var runningWidgetIds = _widgetManager?.RunningWidgetIds;

            var appState = new AppState();
            if (runningWidgetIds != null)
                appState.Widgets = runningWidgetIds.ToList();

            await _stateStore.SaveAsync(appState, APP_STATE);
            
            base.OnExit(e);
        }
    }
}