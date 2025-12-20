using System.Windows;

namespace HTCHome.Widgets
{
    public partial class SettingsWindow : Window
    {
        private readonly Home.Base.Widgets.ISettingsView? _settingsLogic;

        public SettingsWindow(string title, FrameworkElement content)
        {
            InitializeComponent();
            Title = title;
            SettingsContent.Content = content;
            _settingsLogic = content as Home.Base.Widgets.ISettingsView;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _settingsLogic?.OnSave();
            DialogResult = true;
            Close();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _settingsLogic?.OnReset();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _settingsLogic?.OnCancel();
            Close();
        }
    }
}
