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

            SetupContextMenu();

            MouseMove += WidgetWindow_MouseMove;
        }

        private void SetupContextMenu()
        {
            ContextMenu = new ContextMenu
            {
                Items =
                {
                    new MenuItem
                    {
                        Header = "Remove", // TODO: localization
                        Command = new RelayCommand(_ => RemoveRequested?.Invoke(this, EventArgs.Empty))
                    },

                    new MenuItem
                    {
                        Header = "Close", // TODO: localization
                        Command = new RelayCommand(_ => Close())
                    }
                }
            };
        }

        private void WidgetWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
