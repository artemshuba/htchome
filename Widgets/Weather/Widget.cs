using Home.Base.Widgets;
using System.Windows;
using Weather.Windows;

namespace Weather
{
    public class Widget : IWidget
    {
        public FrameworkElement GetView()
        {
            // For now, we reuse the existing WeatherLarge window content.
            // In a full refactor, this should be a UserControl (WidgetView).
            // Since IWidget expects a FrameworkElement, we can return a UserControl that wraps the logic,
            // or modify WeatherLarge to be a UserControl.
            
            return new WeatherLarge();
        }
    }
}
