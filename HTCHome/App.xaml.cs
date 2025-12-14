using HTCHome.Widgets;
using System.Windows;

namespace HTCHome
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var widgetManager = await WidgetManager.CreateAsync();
            widgetManager.LoadWidgetAsync("clock");
        }
    }
}
