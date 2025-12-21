using HTCHome.Services;
using System.Windows;
using System.Windows.Controls;

namespace HTCHome.Widgets
{
    public partial class GlobalSettingsControl : UserControl
    {
        private readonly AutostartService _autostartService;

        public GlobalSettingsControl()
        {
            InitializeComponent();
            _autostartService = new AutostartService();
            CheckAutostart.IsChecked = _autostartService.IsAutostartEnabled;
        }

        private void CheckAutostart_CheckedChanged(object sender, RoutedEventArgs e)
        {
            _autostartService.IsAutostartEnabled = CheckAutostart.IsChecked == true;
        }
    }
}
