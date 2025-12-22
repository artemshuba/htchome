using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Home.Base.Services;

namespace HTCHome.Widgets
{
    public partial class SettingsWindow : Window
    {
        private readonly Home.Base.Widgets.ISettingsView? _settingsLogic;
        private readonly ISkinService? _skinService;

        public SettingsWindow(string title, FrameworkElement? content, ISkinService? skinService)
        {
            InitializeComponent();
            Title = title;
            _skinService = skinService;
            
            if (content != null)
            {
                SettingsContent.Content = content;
                _settingsLogic = content as Home.Base.Widgets.ISettingsView;
            }
            else
            {
                TabWidgetSettings.Visibility = Visibility.Collapsed;
            }

            LoadSkins();
        }

        private void LoadSkins()
        {
            if (_skinService == null)
            {
                GrpAppearance.Visibility = Visibility.Collapsed;
                return;
            }

            ComboSkins.ItemsSource = _skinService.AvailableSkins;
            if (_skinService.CurrentSkin != null)
            {
                ComboSkins.SelectedValue = _skinService.CurrentSkin.Name;
                UpdateSkinInfo(_skinService.CurrentSkin);
            }
        }

        private void UpdateSkinInfo(SkinInfo skin)
        {
            TxtVersion.Text = skin.Version;
            TxtAuthor.Text = skin.Author;
            
            if (!string.IsNullOrEmpty(skin.PreviewPath) && File.Exists(skin.PreviewPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(skin.PreviewPath);
                    bitmap.EndInit();
                    ImgPreview.Source = bitmap;
                }
                catch 
                { 
                    ImgPreview.Source = null; 
                }
            }
            else
            {
                ImgPreview.Source = null;
            }
        }

        private void ComboSkins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboSkins.SelectedItem is SkinInfo skin)
            {
                try
                {
                    _skinService.ApplySkin(skin.Name);
                    UpdateSkinInfo(skin);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error applying skin: {ex.Message}");
                }
            }
        }

        private void BtnEditSkin_Click(object sender, RoutedEventArgs e)
        {
             if (ComboSkins.SelectedItem is SkinInfo skin)
             {
                 try
                 {
                     _skinService.EditSkin(skin.Name);
                 }
                 catch (Exception ex)
                 {
                      MessageBox.Show($"Error opening editor: {ex.Message}");
                 }
             }
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
