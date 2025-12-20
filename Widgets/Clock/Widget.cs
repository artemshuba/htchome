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
            
            // Apply saved skin
            var skin = _context.Configuration.GetValue<string>("Skin");
            if (!string.IsNullOrEmpty(skin))
            {
                _context.SkinService.ApplySkin(skin);
            }
            else
            {
                // Default fallback if needed, though SkinService usually has defaults
                _context.SkinService.ApplySkin("Modern Sense");
            }
        }

        public FrameworkElement CreateSettingsView()
        {
            if (_context == null) return new System.Windows.Controls.Control(); // Should not happen
            return new SettingsControl(_context.Configuration, _context.SkinService);
        }

        public FrameworkElement CreateView() 
        {
            _view = new WidgetView();
            return _view;
        }

        public void Unload()
        {
            _view = null;
            _context = null;
        }
    }
}
