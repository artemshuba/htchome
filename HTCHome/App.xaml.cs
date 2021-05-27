using HTCHome.Widgets;
using System.Windows;

namespace HTCHome
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var widgetManager = new WidgetManager();
            widgetManager.LoadWidgets();
        }
    }
}
