using System.Windows.Controls;

namespace Clock
{
    public partial class SettingsControl : UserControl, Home.Base.Widgets.ISettingsView
    {
        private readonly Home.Base.Services.IConfigurationService _config;
        private readonly Home.Base.Services.ISkinService _skinService;

        public SettingsControl(Home.Base.Services.IConfigurationService config, Home.Base.Services.ISkinService skinService)
        {
            InitializeComponent();
            _config = config;
            _skinService = skinService;
            LoadSettings();
            LoadSkins();
        }

        private void LoadSkins()
        {
            ComboSkins.ItemsSource = _skinService.AvailableSkins;
            if (_skinService.CurrentSkin != null) 
            {
               ComboSkins.SelectedItem = _skinService.CurrentSkin;
            }
        }

        private void ComboSkins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             if (ComboSkins.SelectedItem is string skinName)
             {
                 _skinService.ApplySkin(skinName);
             }
        }

        private void LoadSettings()
        {
            CheckShowSeconds.IsChecked = _config.GetValue<bool>("ShowSeconds");
            Check24Hour.IsChecked = _config.GetValue<bool>("Is24Hour");
            
            // If persisted skin exists, apply it?
            // WidgetSkinService defaults to "Default", but maybe we should load from config here?
            // Ideally ISkinService handles persistence logic itself, but for now we do it here.
            var skin = _config.GetValue<string>("Skin");
            if (!string.IsNullOrEmpty(skin) && skin != _skinService.CurrentSkin)
            {
                _skinService.ApplySkin(skin);
                ComboSkins.SelectedItem = skin;
            }
        }

        public void OnSave()
        {
            _config.SetValue("ShowSeconds", CheckShowSeconds.IsChecked == true);
            _config.SetValue("Is24Hour", Check24Hour.IsChecked == true);
            
            if (ComboSkins.SelectedItem is string skinName)
            {
               _config.SetValue("Skin", skinName);
            }
            
            _config.SaveAsync().ConfigureAwait(false); 
        }

        public void OnReset()
        {
            _config.SetValue<bool?>("ShowSeconds", null);
            _config.SetValue<bool?>("Is24Hour", null);
             _config.SetValue<string?>("Skin", null);
             
             _skinService.ApplySkin("Default");
             ComboSkins.SelectedItem = "Default";
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
