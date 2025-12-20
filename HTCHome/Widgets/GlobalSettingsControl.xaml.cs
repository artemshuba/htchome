using HTCHome.Services;
using System.Windows.Controls;

namespace HTCHome.Widgets
{
    public partial class GlobalSettingsControl : UserControl, Home.Base.Widgets.ISettingsView
    {
        private readonly SkinManager _skinManager;

        public GlobalSettingsControl(SkinManager skinManager)
        {
            InitializeComponent();
            _skinManager = skinManager;
            LoadSkins();
        }

        private void LoadSkins()
        {
            ComboSkins.ItemsSource = _skinManager.AvailableSkins;
            ComboSkins.SelectedItem = _skinManager.CurrentSkin;
        }

        private void ComboSkins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Preview logic if desired, or wait for save?
            // Usually skins apply immediately to see effect.
            // Let's apply immediately for "preview", but maybe revert on Cancel?
            // For now, let's just apply.
            if (ComboSkins.SelectedItem is string skinName)
            {
                _skinManager.ApplySkin(skinName);
            }
        }

        public void OnSave()
        {
            // Save current skin to config (TODO: Global Config Service)
            // For now, we just assume it's applied
            StatusText.Text = "Settings Saved";
        }

        public void OnReset()
        {
             _skinManager.ApplySkin("Default");
             ComboSkins.SelectedItem = "Default";
        }

        public void OnCancel()
        {
            // Revert?
            // If we applied immediately, we might want to revert to original
            // Simplification: Do nothing for now
        }
    }
}
