using System.Windows.Controls;

namespace Clock
{
    public partial class SettingsControl : UserControl, Home.Base.Widgets.ISettingsView
    {
        private readonly Home.Base.Services.IConfigurationService _config;

        public SettingsControl(Home.Base.Services.IConfigurationService config)
        {
            InitializeComponent();
            _config = config;
            LoadSettings();
        }

        private void LoadSettings()
        {
            CheckShowSeconds.IsChecked = _config.GetValue<bool>("ShowSeconds");
            Check24Hour.IsChecked = _config.GetValue<bool>("Is24Hour");
        }

        public void OnSave()
        {
            _config.SetValue("ShowSeconds", CheckShowSeconds.IsChecked == true);
            _config.SetValue("Is24Hour", Check24Hour.IsChecked == true);
            
            _config.SaveAsync().ConfigureAwait(false); 
        }

        public void OnReset()
        {
            _config.SetValue<bool?>("ShowSeconds", null);
            _config.SetValue<bool?>("Is24Hour", null);
             
            LoadSettings();
        }

        public void OnCancel()
        {
            // Revert changes logic is complex with immediate apply.
            // For now just reload settings.
            LoadSettings();
        }
    }
}
