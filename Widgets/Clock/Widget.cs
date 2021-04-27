using Home.Base.Widgets;
using System.Windows;

namespace Clock
{
    public class Widget : IWidget
    {
        public FrameworkElement GetView() => new WidgetView();
    }
}
