using Clock.ViewModel;
using System;
using System.Windows.Controls;

namespace Clock
{
    /// <summary>
    /// Interaction logic for WidgetView.xaml
    /// </summary>
    public partial class WidgetView : UserControl
    {
        public WidgetViewModel ViewModel => (WidgetViewModel)DataContext;

        public WidgetView()
        {
            InitializeComponent();

            MinutesTab.Flip(DateTime.Now.AddMinutes(-2).Minute, animated: false);
            HoursTab.Flip(DateTime.Now.AddHours(-1).Hour, animated: false);

            HoursTab.FlipCompleted += HoursTab_FlipCompleted;
            MinutesTab.FlipCompleted += MinutesTab_FlipCompleted;
        }

        private void Widget_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            HoursTab.Flip(DateTime.Now.Hour);
            MinutesTab.Flip(DateTime.Now.AddMinutes(-1).Minute);
        }

        private void HoursTab_FlipCompleted(object sender, EventArgs e)
        {
            HoursTab.Delay = 0;
            HoursTab.FlipCompleted -= HoursTab_FlipCompleted;
        }

        private void MinutesTab_FlipCompleted(object sender, EventArgs e)
        {
            MinutesTab.Delay = 0;
            MinutesTab.FlipCompleted -= MinutesTab_FlipCompleted;

            MinutesTab.Flip(DateTime.Now.Minute);
        }
    }
}