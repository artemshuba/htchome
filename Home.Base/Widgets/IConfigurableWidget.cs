using System.Windows;

namespace Home.Base.Widgets
{
    public interface IConfigurableWidget
    {
        /// <summary>
        /// Creates and returns the settings view for this widget instance.
        /// The view should handle loading and saving settings using the IWidgetContext.Configuration service.
        /// </summary>
        /// <returns>A FrameworkElement (e.g. UserControl) containing the settings UI.</returns>
        FrameworkElement CreateSettingsView();
    }
}
