using System.Windows.Controls;
using Home.Base.Widgets;
using Weather.Base;
using System.Linq;

namespace Clock
{
    public partial class SettingsControl : UserControl, ISettingsView
    {
        private readonly IWidgetContext _context;

        public SettingsControl(IWidgetContext context)
        {
            InitializeComponent();
            _context = context;
            LoadSettings();
        }

        private void LoadSettings()
        {
            CheckShowSeconds.IsChecked = _context.Configuration.GetValue<bool>("ShowSeconds");
            Check24Hour.IsChecked = _context.Configuration.GetValue<bool>("Is24Hour");
            
            // Load Providers
            var providers = _context.GetExtensions<IWeatherProvider>().ToList();
            ComboProviders.ItemsSource = providers;
            
            var currentProvider = _context.Configuration.GetValue<string>("WeatherProvider");
            if (!string.IsNullOrEmpty(currentProvider))
            {
                ComboProviders.SelectedValue = currentProvider;
            }
            else
            {
                ComboProviders.SelectedIndex = 0;
            }

            TxtCity.Text = _context.Configuration.GetValue<string>("WeatherCity") ?? "New York";
        }

        public void OnSave()
        {
            _context.Configuration.SetValue("ShowSeconds", CheckShowSeconds.IsChecked == true);
            _context.Configuration.SetValue("Is24Hour", Check24Hour.IsChecked == true);
            
            if (ComboProviders.SelectedValue is string providerName)
            {
                _context.Configuration.SetValue("WeatherProvider", providerName);
            }
            _context.Configuration.SetValue("WeatherCity", TxtCity.Text);
            
            _context.Configuration.SaveAsync().ConfigureAwait(false); 
        }

        public void OnReset()
        {
            _context.Configuration.SetValue<bool?>("ShowSeconds", null);
            _context.Configuration.SetValue<bool?>("Is24Hour", null);
            _context.Configuration.SetValue<string>("WeatherProvider", null);
            _context.Configuration.SetValue<string>("WeatherCity", null);
             
            LoadSettings();
        }

        public void OnCancel()
        {
            LoadSettings();
        }
    }
}
