using Home.Base.Mvvm;
using Home.Base.Widgets;
using System;
using System.Linq;
using System.Windows.Threading;
using Weather.Base;
using System.Globalization;

namespace Clock.ViewModel
{
    public class WidgetViewModel : BindableBase
    {
        private readonly IWidgetContext _context;
        private DispatcherTimer _clockTimer;
        private DispatcherTimer _weatherTimer;
        private IWeatherProvider? _weatherProvider;

        private int _hours;
        private int _minutes;
        private int _seconds;
        private double _hourAngle;
        private double _minuteAngle;
        private double _secondAngle;
        private string _dateString;
        private string _amPm;

        // Weather Properties
        private string _weatherCity = "Select City";
        private string _weatherCondition = "";
        private string _weatherTemp = "";
        private string _weatherIcon;

        public int Hours { get { return _hours; } set { Set(ref _hours, value); } }
        public int Minutes { get { return _minutes; } set { Set(ref _minutes, value); } }
        public int Seconds { get { return _seconds; } set { Set(ref _seconds, value); } }
        
        // Angles for Analog Clock
        public double HourAngle { get { return _hourAngle; } set { Set(ref _hourAngle, value); } }
        public double MinuteAngle { get { return _minuteAngle; } set { Set(ref _minuteAngle, value); } }
        public double SecondAngle { get { return _secondAngle; } set { Set(ref _secondAngle, value); } }

        public string DateString { get { return _dateString; } set { Set(ref _dateString, value); } }
        public string AmPm { get { return _amPm; } set { Set(ref _amPm, value); } }

        public string WeatherCity { get { return _weatherCity; } set { Set(ref _weatherCity, value); } }
        public string WeatherCondition { get { return _weatherCondition; } set { Set(ref _weatherCondition, value); } }
        public string WeatherTemp { get { return _weatherTemp; } set { Set(ref _weatherTemp, value); } }
        
        // Full Pack URI for the icon
        public string WeatherIcon { get { return _weatherIcon; } set { Set(ref _weatherIcon, value); } }

        public WidgetViewModel(IWidgetContext context)
        {
            _context = context;
            UpdateClock();
            _clockTimer = new DispatcherTimer(TimeSpan.FromSeconds(0.1), DispatcherPriority.Normal, (s, e) => UpdateClock(), Dispatcher.CurrentDispatcher);
            
            // Initialize Weather
            LoadWeatherProvider();
            UpdateWeather();
            _weatherTimer = new DispatcherTimer(TimeSpan.FromMinutes(30), DispatcherPriority.Background, (s, e) => UpdateWeather(), Dispatcher.CurrentDispatcher);
        }

        private void LoadWeatherProvider()
        {
            var providerName = _context.Configuration.GetValue<string>("WeatherProvider");
            var providers = _context.GetExtensions<IWeatherProvider>();
            
            if (!string.IsNullOrEmpty(providerName))
            {
                _weatherProvider = providers.FirstOrDefault(p => p.GetType().Name == providerName || 
                                                                 (p is WeatherProvider wp && wp.Name == providerName));
                // Fallback: Check if Name property matches (ExtensionWrapper)
            }
            
            if (_weatherProvider == null)
            {
                _weatherProvider = providers.FirstOrDefault();
            }
        }

        private async void UpdateWeather()
        {
            if (_weatherProvider == null) return;
            
            try
            {
               var city = _context.Configuration.GetValue<string>("WeatherCity");
               if (string.IsNullOrEmpty(city)) city = "New York"; 
               
               // Mock location data for now since we don't have location search UI yet
               var locData = new LocationData { City = city, Code = city }; 
               
               var culture = CultureInfo.CurrentCulture;
               // Try to search functionality if provider supports it effectively
               try 
               {
                    var locations = _weatherProvider.GetLocations(city, culture);
                    if (locations != null && locations.Count > 0)
                    {
                        locData = locations[0];
                    }
               }
               catch { /* Provider search might fail or not be implemented fully */ }

               WeatherCity = locData.City;
               
               var report = _weatherProvider.GetWeatherReport(culture, locData, TemperatureScale.Celsius, WindSpeedScale.Ms, TimeSpan.Zero);
               if (report != null)
               {
                    WeatherTemp = $"{report.Temperature}°";
                    WeatherCondition = report.Curent?.Text ?? "";
                    
                    if (report.Curent != null)
                    {
                        WeatherIcon = $"pack://application:,,,/Clock;component/Resources/Weather/weather_{report.Curent.SkyCode}.png";
                    }
               }
            }
            catch (Exception ex)
            {
                _context.Logger.Error("Failed to update weather", ex);
                WeatherCondition = "Error";
            }
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            Hours = now.Hour;
            Minutes = now.Minute;
            Seconds = now.Second;

            DateString = now.ToString("ddd, MMM d").ToUpper(); // e.g. MON, JAN 1
            AmPm = now.ToString("tt");

            // Analog Logic
            // Hour hand moves with minutes: 30 degrees per hour + 0.5 degrees per minute
            HourAngle = (now.Hour % 12) * 30 + now.Minute * 0.5;
            // Minute hand moves with seconds: 6 degrees per minute + 0.1 degrees per second
            MinuteAngle = now.Minute * 6 + now.Second * 0.1;
            // Second hand: 6 degrees per second
            SecondAngle = now.Second * 6;
        }
    }
}
