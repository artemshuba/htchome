using System;
using System.Windows;
using Home.Base.Services;

namespace HTCHome.Widgets
{
    public partial class SkinEditorWindow : Window
    {
        private readonly ISkinService _skinService;
        private readonly string _skinName;

        public SkinEditorWindow(ISkinService skinService, string skinName)
        {
            InitializeComponent();
            _skinService = skinService;
            _skinName = skinName;

            SkinNameTextBlock.Text = _skinName;
            LoadContent();
        }

        private async void LoadContent()
        {
            EditorTextBox.Text = "Loading...";
            EditorTextBox.IsEnabled = false;

            try
            {
               var content = await _skinService.GetSkinContentAsync(_skinName);
               EditorTextBox.Text = content;
               EditorTextBox.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading skin: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EditorTextBox.IsEnabled = false;
                await _skinService.SaveSkinContentAsync(_skinName, EditorTextBox.Text);
                
                // Re-apply immediately
                _skinService.ApplySkin(_skinName);
            }
            catch (Exception ex)
            {
                 MessageBox.Show($"Error saving skin: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EditorTextBox.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
