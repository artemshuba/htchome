using System.Windows;

namespace Home.Base.Widgets
{
    public class WidgetInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public interface IWidget
    {
        WidgetInfo Info { get; }
        void Initialize(IWidgetContext context);
        FrameworkElement CreateView();
        void Unload();
    }
}
