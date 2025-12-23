using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Weather.Base;

namespace Weather.Domain
{
    public class WeatherProviderManager
    {
        public WeatherProvider CurrentProvider { get; set; }

        public List<WeatherProvider> Providers { get; private set; }

        public void LoadProviders(IEnumerable<IWeatherProvider> extensions)
        {
            Providers = new List<WeatherProvider>();
            foreach (var ext in extensions)
            {
                var p = new WeatherProvider(ext);
                // p.Load(); // No-op now
                
                if (Globals.Settings.Provider == p.Name)
                {
                    CurrentProvider = p;
                }
                Providers.Add(p);
            }
        }
    }
}
