using Home.Base.Widgets;
using System.Windows;
using Weather.Windows;

namespace Weather
{
    public class Widget : IWidget
    {
        private FrameworkElement? _view;

        public WidgetInfo Info => new WidgetInfo 
        { 
            Name = "Weather Widget", 
            Description = "HTC Weather Widget", 
            Version = "1.0", 
            Author = "HTC Home Team" 
        };

        public void Initialize(IWidgetContext context)
        {
            // Initialize
        }

        public FrameworkElement CreateView()
        {
            // For now, we reuse the existing WeatherLarge window content.
            // In a full refactor, this should be a UserControl (WidgetView).
            // Since IWidget expects a FrameworkElement, we can return a UserControl that wraps the logic,
            // or modify WeatherLarge to be a UserControl.
            
            _view = new WeatherLarge();
            return _view;
        }

        public void Unload()
        {
            _view = null;
        }
    }
}
