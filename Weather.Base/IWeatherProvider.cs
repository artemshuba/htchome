using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Weather.Base
{
    public interface IWeatherProvider : Home.Base.Widgets.IExtension
    {
        /// <summary>
        /// Get location list
        /// </summary>
        /// <param name="query">Location Name</param>
        /// <param name="culture">Culture info</param>
        /// <returns></returns>
        List<LocationData> GetLocations(string query, CultureInfo culture);
        ///// <summary>
        ///// Get location list
        ///// </summary>
        ///// <param name="query">Location Name</param>
        ///// <param name="culture">Culture info</param>
        ///// <param name="tempScale">Fahrenheit or Celsius</param>
        ///// <returns></returns>
        //List<LocationData> GetLocations(string query, CultureInfo culture, TemperatureScale tempScale = TemperatureScale.Celsius);

        /// <summary>
        /// Get weather report
        /// </summary>
        /// <param name="culture">Culture info (weather report language)</param>
        /// <param name="location">Location code</param>
        /// <param name="tempScale">Fahrenheit or Celsius</param>
        /// <param name="windSpeedScale">Mile per hour, kilometers per hour or meters per second</param>
        /// <param name="baseUtcOffset">Uses for get right weather pic for non-system timezone</param>
        /// <returns></returns>
        WeatherData GetWeatherReport(CultureInfo culture, LocationData location, TemperatureScale tempScale, WindSpeedScale windSpeedScale, TimeSpan baseUtcOffset);
    }
}
