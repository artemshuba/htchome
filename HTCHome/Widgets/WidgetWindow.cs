using Home.Base.Mvvm;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HTCHome.Widgets
{
    public class WidgetWindow : Window
    {
        public event EventHandler? RemoveRequested;

        public WidgetWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true; // Use WPF Transparency by default (Better for PNGs/Shaped windows on all OS)
            Background = new SolidColorBrush(Colors.Transparent);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;

            SetupContextMenu();

            MouseLeftButtonDown += WidgetWindow_MouseLeftButtonDown;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            UpdateWindowEffects();
        }

        public void UpdateWindowEffects()
        {
            // 1. Check backward compatibility (Win 7 Aero Blur)
            // If the current Skin defines a "WidgetGlassRegion" (Geometry), apply it.
            var glassRegion = TryFindResource("WidgetGlassRegion") as Geometry;
            
            if (glassRegion != null)
            {
                 // Native effects usually need AllowsTransparency=true + DwmBlurBehind for Region on Win7?
                 // Actually, on Win7, to have non-client blur behind specific region of a layered window:
                 // DwmEnableBlurBehindWindow works.
                 // We rely on WindowEffects to safely check OS version (only Win7/8).
                 Utils.WindowEffects.EnableBlurBehind(this, glassRegion);
            }
            else
            {
                Utils.WindowEffects.DisableBlurBehind(this);
            }
        }

        private void WidgetWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        public event EventHandler? SettingsRequested;
        public event EventHandler? GlobalSettingsRequested;
        public event EventHandler? ExitRequested;

        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenu();
            
            // Add Widget (Placeholder, populated by Manager)
            var addWidgetMenuItem = new MenuItem { Header = "Add Widget" };
            contextMenu.Items.Add(addWidgetMenuItem);

            contextMenu.Items.Add(new Separator());

            // Widget Settings
            contextMenu.Items.Add(new MenuItem 
            { 
                Header = "Widget Settings", 
                Command = new RelayCommand(_ => SettingsRequested?.Invoke(this, EventArgs.Empty)) 
            });

            // HTC Home Settings
            contextMenu.Items.Add(new MenuItem 
            { 
                Header = "HTC Home Settings", 
                Command = new RelayCommand(_ => GlobalSettingsRequested?.Invoke(this, EventArgs.Empty)) 
            });

            // Remove
            contextMenu.Items.Add(new MenuItem
            {
                Header = "Remove Widget",
                Command = new RelayCommand(_ => RemoveRequested?.Invoke(this, EventArgs.Empty))
            });
            
            contextMenu.Items.Add(new Separator());

            // Exit
            contextMenu.Items.Add(new MenuItem
            {
                Header = "Exit HTC Home",
                Command = new RelayCommand(_ => ExitRequested?.Invoke(this, EventArgs.Empty))
            });

            ContextMenu = contextMenu;
        }


    }
}
