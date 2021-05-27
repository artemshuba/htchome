using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HTCHome.Widgets
{
    public class WidgetWindow : Window
    {
        public WidgetWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Colors.Transparent);

            MouseMove += WidgetWindow_MouseMove;
        }

        private void WidgetWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
