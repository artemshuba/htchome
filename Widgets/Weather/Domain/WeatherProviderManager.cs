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

        public void FindProviders()
        {
            if (Directory.Exists(Home.Base.E.ExtPath + "\\Weather"))
            {
                Providers = new List<WeatherProvider>();
                var files = from x in Directory.GetFiles(Home.Base.E.ExtPath + "\\Weather")
                            where x.EndsWith(".dll")
                            select x;
                foreach (var f in files)
                {
                    var p = new WeatherProvider(f);
                    if (Globals.Settings.Provider == p.Name)
                    {
                        CurrentProvider = p;
                        p.Load();
                        if (p.HasErrors)
                        {
                            CurrentProvider = null;
                            continue;
                        }
                    }
                    Providers.Add(p);
                }
            }
        }
    }
}
