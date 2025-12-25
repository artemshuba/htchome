using Home.Base.Widgets;
using System.Windows;

namespace Clock
{
    public class Widget : IWidget, IConfigurableWidget
    {
        private WidgetView? _view;
        private IWidgetContext? _context;

        public WidgetInfo Info => new WidgetInfo 
        { 
            Name = "Flip Clock", 
            Description = "Classic HTC Flip Clock", 
            Version = "1.0", 
            Author = "HTC Home Team" 
        };

        public void Initialize(IWidgetContext context)
        {
            _context = context;
        }

        public FrameworkElement CreateSettingsView()
        {
            if (_context == null) return new System.Windows.Controls.Control(); 
            return new SettingsControl(_context);
        }

        public FrameworkElement CreateView() 
        {
            _view = new WidgetView();
            _view.DataContext = new ViewModel.WidgetViewModel(_context);
            return _view;
        }

        public void Unload()
        {
            _view = null;
            _context = null;
        }
    }
}
