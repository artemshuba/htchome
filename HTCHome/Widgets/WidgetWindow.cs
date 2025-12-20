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
            AllowsTransparency = true;
            Background = new SolidColorBrush(Colors.Transparent);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;

            SetupContextMenu();

            MouseMove += WidgetWindow_MouseMove;
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

        private void WidgetWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
