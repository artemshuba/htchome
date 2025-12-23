using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Home.Base;
using Weather.Base;
using Weather.Controls;
using Weather.Domain;
using System.Windows.Media.Animation;
using System.IO;

namespace Weather.Windows
{
    /// <summary>
    /// Interaction logic for WeatherWidget.xaml
    /// </summary>
    public partial class WeatherLarge : UserControl
    {
        private WeatherData currentWeather;
        private LocationData currentLocation;
        private DispatcherTimer weatherTimer;
        private Options optionsWindow;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private WeatherState lastWeatherState;

        public WeatherLarge()
        {
            InitializeComponent();
        }

        private void WindowSourceInitialized(object sender, EventArgs e)
        {
            // Window-specific logic removed as this is now a UserControl hosted in a window
            
            if (Globals.Settings.UseAero)
            {
                // UpdateAero(); // Aero logic needs window handle, might need refactor if still required
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize Globals if not already
            // Fixed: Check removed

            currentLocation = new LocationData();
            currentLocation.Code = Globals.Settings.LocationCode;

            currentWeather = (WeatherData)XmlSerializable.Load(typeof(WeatherData), E.Root + "\\Weather.data") ?? new WeatherData();
            lastWeatherState = WeatherConverter.ConvertSkyCodeToWeatherState(currentWeather.Curent.SkyCode);

            if (string.IsNullOrEmpty(currentLocation.Code))
            {
                WeatherGrid.Visibility = System.Windows.Visibility.Collapsed;
                TempGrid.Visibility = System.Windows.Visibility.Collapsed;
                ForecastPanel.Visibility = System.Windows.Visibility.Collapsed;
                WeatherIcon.Visibility = System.Windows.Visibility.Collapsed;
                SetupLocationTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                WeatherGrid.Visibility = System.Windows.Visibility.Visible;
                TempGrid.Visibility = System.Windows.Visibility.Visible;
                ForecastPanel.Visibility = System.Windows.Visibility.Visible;
                WeatherIcon.Visibility = System.Windows.Visibility.Visible;
                SetupLocationTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            }

            UpdateWeatherUI();
            lastWeatherState = WeatherState.None;

            weatherTimer = new DispatcherTimer();
            weatherTimer.Interval = TimeSpan.FromMinutes(Globals.Settings.RefreshInterval);
            weatherTimer.Tick += WeatherTimerTick;
            weatherTimer.Start();

            if (Globals.WpManager.CurrentProvider == null)
            {
                Globals.Settings.Provider = "MSN";
                Globals.WpManager.CurrentProvider = Globals.WpManager.Providers.Find(p => p.Name == "MSN");
                if (Globals.WpManager.CurrentProvider != null)
                    Globals.WpManager.CurrentProvider.Load();
            }

            if (currentLocation.Code != null)
                RefreshWeather();
        }

        private void WeatherTimerTick(object sender, EventArgs e)
        {
            RefreshWeather();
        }

        private void RefreshWeather()
        {
            WeatherRefreshProgressBar.Visibility = System.Windows.Visibility.Visible;
            // Taskbar.ProgressState = TaskbarItemProgressState.Indeterminate; // Taskbar logic removed
            ThreadStart threadStarter = () =>
            {
                logger.Info("Getting weather report");
                var w = Globals.WpManager.CurrentProvider.GetWeatherReport(CultureInfo.GetCultureInfo(Globals.Settings.Language), currentLocation,
                        Globals.Settings.TempScale, Globals.Settings.WindSpeedScale, TimeZoneInfo.Local.BaseUtcOffset);
                if (w != null)
                {
                    logger.Info("Got weather report:");
                    logger.Info("Location: {0}", w.Location.Code);
                    logger.Info("Temperature: {0}", w.Temperature);
                    logger.Info("Feels like: {0}", w.FeelsLike);
                    logger.Info("Humidity: {0}", w.Humidity);
                    logger.Info("Wind speed: {0}", w.WindSpeed);
                    logger.Info("Skycode: {0}", w.Curent.SkyCode);
                    logger.Info("Text: {0}", w.Curent.Text);

                    currentWeather = w;

                    logger.Info("Updating weather UI");
                    UpdateWeatherUI();
                    logger.Info("Weather UI updated");
                }
                else
                    logger.Info("Weather report is null");

                WeatherRefreshProgressBar.Dispatcher.Invoke((Action)delegate
                {
                    WeatherRefreshProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                });

                currentWeather.Save(E.Root + "\\Weather.data");
            };
            var thread = new Thread(threadStarter);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void UpdateWeatherUI()
        {
            WeatherGrid.Dispatcher.Invoke((Action)delegate
           {
               LocationTextBlock.Text = currentWeather.Location.City;
               WeatherTextBlock.Text = currentWeather.Curent.Text;

               var state = WeatherConverter.ConvertSkyCodeToWeatherState(currentWeather.Curent.SkyCode);
               if (state != lastWeatherState && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
               {
                   SetWeatherState(state);
               }


               // // this.Icon = WeatherIcon.Source; // Removed Window property
               // if (!string.IsNullOrEmpty(currentWeather.Curent.Text))
               //     // this.Title = currentWeather.Curent.Text; // Removed Window property
           });
            this.Dispatcher.Invoke((Action) delegate
            {
                // // // App.UpdateOverlayIcon(currentWeather.Temperature); // Removed OverlayIcon logic
            });
            TempGrid.Dispatcher.Invoke((Action)delegate
            {
                if (Globals.Settings.ShowFeelsLike)
                    TemperatureTextBlock.Text = currentWeather.FeelsLike + "°";
                else
                    TemperatureTextBlock.Text = currentWeather.Temperature + "°";

                if (currentWeather.ForecastList.Count > 0)
                {
                    TemperatureHLTextBlock.Text = currentWeather.ForecastList[0].HighTemperature + "°" + " / " + currentWeather.ForecastList[0].LowTemperature + "°";
                }
            });

            HWGrid.Dispatcher.Invoke((Action)delegate
           {
               if (!Globals.Settings.ShowHW || string.IsNullOrEmpty(Globals.Settings.LocationCode))
                   HWGrid.Visibility = System.Windows.Visibility.Collapsed;
               else
               {
                   HWGrid.Visibility = System.Windows.Visibility.Visible;

                   switch (Globals.Settings.WindSpeedScale)
                   {
                       case WindSpeedScale.Mph:
                           WindSpeedValueTextBlock.Text = currentWeather.WindSpeed + " " + Properties.Resources.Mph;
                           break;
                       case WindSpeedScale.Kmh:
                           WindSpeedValueTextBlock.Text = currentWeather.WindSpeed + " " + Properties.Resources.Kmh;
                           break;
                       case WindSpeedScale.Ms:
                           WindSpeedValueTextBlock.Text = currentWeather.WindSpeed + " " + Properties.Resources.Ms;
                           break;
                   }

                   var windSpeedKmh = (int)WeatherConverter.WindSpeedConvertToKmh(currentWeather.WindSpeed, Globals.Settings.WindSpeedScale);

                   if (windSpeedKmh < 20)
                   {
                       WindSpeedIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/wind_lvl1.png", UriKind.Relative));
                   }

                   if (windSpeedKmh >= 20 && windSpeedKmh < 50)
                   {
                       WindSpeedIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/wind_lvl2.png", UriKind.Relative));
                   }

                   if (windSpeedKmh >= 50 && windSpeedKmh < 88)
                   {
                       WindSpeedIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/wind_lvl3.png", UriKind.Relative));
                   }
                   if (windSpeedKmh >= 88)
                   {
                       WindSpeedIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/wind_lvl4.png", UriKind.Relative));
                   }

                   HumidityValueTextBlock.Text = currentWeather.Humidity + "%";

                   if (currentWeather.Humidity < 25)
                       HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl1.png", UriKind.Relative));
                   if (currentWeather.Humidity >= 25 && currentWeather.Humidity < 50)
                       HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl2.png", UriKind.Relative));
                   if (currentWeather.Humidity >= 50 && currentWeather.Humidity < 75)
                       HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl3.png", UriKind.Relative));
                   if (currentWeather.Humidity >= 75)
                       HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl4.png", UriKind.Relative));
               }
           });

            WeatherIcon.Dispatcher.Invoke((Action)delegate
           {
               WeatherIcon.Source = new BitmapImage(new Uri(string.Format("/UIFramework.Weather;Component/Images/weather_{0}.png",
                  currentWeather.Curent.SkyCode), UriKind.Relative));
           });


            ForecastPanel.Dispatcher.Invoke((Action)delegate
            {
                if (currentWeather.ForecastList.Count >= 5)
                {
                    var i = 1;
                    foreach (var item in /*forecastWindow.*/ForecastPanel.Children)
                    {
                        if (item.GetType() == typeof(ForecastItem))
                        {
                            var forecastItem = (ForecastItem)item;
                            forecastItem.Temperature.Text = currentWeather.ForecastList[i].HighTemperature + "°/" +
                                currentWeather.ForecastList[i].LowTemperature + "°";
                            forecastItem.DayName.Text = DateTime.Now.AddDays(i).ToString("ddd").ToLower();
                            forecastItem.Icon.Source = new BitmapImage(new Uri(string.Format("/UIFramework.Weather;Component/Images/weather_{0}.png",
                                            currentWeather.ForecastList[i].SkyCode), UriKind.Relative));
                            forecastItem.ToolTip = currentWeather.ForecastList[i].Text + "\n" + Properties.Resources.ForecastTooltip;
                            forecastItem.Url = currentWeather.ForecastList[i].Url;
                            i++;
                        }
                    }
                }

            });
        }


        private void WindowMouseMove(object sender, MouseEventArgs e)
        {
            // DragMove removed logic
        }

        private void WindowMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Globals.Settings.Left = // this.Left;
            // Globals.Settings.Top = // this.Top;
            Globals.Settings.Save(Globals.ConfigFile);
        }

        private void RefreshItemClick(object sender, RoutedEventArgs e)
        {
            RefreshWeather();
        }

        private void OptionsItemClick(object sender, RoutedEventArgs e)
        {
            if (optionsWindow != null && optionsWindow.IsVisible)
            {
                optionsWindow.Activate();
                return;
            }

            optionsWindow = new Options();
            optionsWindow.UpdateSettings += OptionsWindowUpdateSettings;

            optionsWindow.Width = Globals.Settings.OptionsWidth;
            optionsWindow.Height = Globals.Settings.OptionsHeight;

            if (Globals.Settings.Language == "he-IL" || Globals.Settings.Language == "ar-SA")
            {
                optionsWindow.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                optionsWindow.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            }

            optionsWindow.ShowDialog();
        }

        private void OptionsWindowUpdateSettings(object sender, EventArgs e)
        {
            if (sender != null)
                currentLocation = (LocationData)sender;
            if (currentLocation.Code != null)
            {
                RefreshWeather();
            }

            if (weatherTimer.Interval.Minutes != Globals.Settings.RefreshInterval)
            {
                weatherTimer.Interval = TimeSpan.FromMinutes(Globals.Settings.RefreshInterval);
                weatherTimer.Stop();
                weatherTimer.Start();
            }

            if (string.IsNullOrEmpty(currentLocation.City) || string.IsNullOrEmpty(currentLocation.Code))
            {
                WeatherGrid.Visibility = System.Windows.Visibility.Collapsed;
                TempGrid.Visibility = System.Windows.Visibility.Collapsed;
                ForecastPanel.Visibility = System.Windows.Visibility.Collapsed;
                WeatherIcon.Visibility = System.Windows.Visibility.Collapsed;
                SetupLocationTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                WeatherGrid.Visibility = System.Windows.Visibility.Visible;
                TempGrid.Visibility = System.Windows.Visibility.Visible;
                ForecastPanel.Visibility = System.Windows.Visibility.Visible;
                WeatherIcon.Visibility = System.Windows.Visibility.Visible;
                SetupLocationTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            }

            Scale.ScaleX = Globals.Settings.Scale;
            if (Globals.Settings.UseAero)
            {
                // UpdateAero();
            }
            else
            {
                // // Dwm.RemoveGlassRegion(ref handle);
            }

            // this.Opacity = Globals.Settings.Opacity;
        }

        private void CloseItemClick(object sender, RoutedEventArgs e)
        {
            currentWeather.Save(E.Root + "\\Weather.data");
            // // this.Close(); // Cannot close UserControl
        }

        private void SunMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //SomeClouds.FadeOut();
            //Sun.FadeIn();
        }

        private void SunMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //SomeClouds.FadeIn();
            //Sun.FadeOut();
        }

        private void UpdateAero()
        {
            // Aero logic removed for UserControl
        }

        private void TopMostItemClick(object sender, RoutedEventArgs e)
        {
            if (Globals.Settings.TopMost)
            {
                Globals.Settings.TopMost = false;
                // // // this.Topmost = false;
            }
            else
            {
                Globals.Settings.TopMost = true;
                // // // this.Topmost = true;
            }
        }

        private void SetWeatherState(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.Clouds:
                    StartCloudAnimation();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_clouds.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.PartlyCloud:
                    StartPartlyCloudAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_clouds.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.PartlySunny:
                    StartPartlySunnyAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_clouds.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.HeavyRain:
                    StartRainAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_showers.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.SmallRain:
                    StartRainAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_showers.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.Storm:
                    StartLightningAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_thunder.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.Clear:
                    StartClearAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_sunny.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.Fog:
                    StartFogAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_fog.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.Wind:
                    StartWindAnim();
                    if (Globals.SoundPlayer != null && Globals.Settings.EnableSounds && !string.IsNullOrEmpty(Globals.Settings.LocationCode))
                    {
                        Globals.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_windy.wav";
                        Globals.SoundPlayer.Play();
                    }
                    break;
            }
        }



        private void StartClearAnim()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_sunny.mp4") || !File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_clear.mp4"))
                return;
            var calc = new SunCalculator(DateTime.Now, currentWeather.Location.Lat, currentWeather.Location.Lon);
            var isDay = DateTime.Now > calc.DSunRise && DateTime.Now < calc.DSunSet;
            if (isDay)
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_sunny.mp4");
            else
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_clear.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }

        private void StartFogAnim()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_fog_day.mp4") || !File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_fog_night.mp4"))
                return;
            var calc = new SunCalculator(DateTime.Now, currentWeather.Location.Lat, currentWeather.Location.Lon);
            var isDay = DateTime.Now > calc.DSunRise && DateTime.Now < calc.DSunSet;
            if (isDay)
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_fog_day.mp4");
            else
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_fog_night.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }

        private void StartWindAnim()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_windy_day.mp4") || !File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_windy_night.mp4"))
                return;
            var calc = new SunCalculator(DateTime.Now, currentWeather.Location.Lat, currentWeather.Location.Lon);
            var isDay = DateTime.Now > calc.DSunRise && DateTime.Now < calc.DSunSet;
            if (isDay)
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_windy_day.mp4");
            else
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_windy_night.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }

        private void StartPartlyCloudAnim()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_partly_cloud.mp4") || !File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_partly_cloud_night.mp4"))
                return;
            var calc = new SunCalculator(DateTime.Now, currentWeather.Location.Lat, currentWeather.Location.Lon);
            var isDay = DateTime.Now > calc.DSunRise && DateTime.Now < calc.DSunSet;
            if (isDay)
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_partly_cloud.mp4");
            else
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_partly_cloud_night.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }

        private void StartPartlySunnyAnim()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_partly_sunny.mp4"))
                return;
            MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_partly_sunny.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }


        private void StartCloudAnimation()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_cloudy_day.mp4") || !File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_cloudy_night.mp4"))
                return;
            var calc = new SunCalculator(DateTime.Now, currentWeather.Location.Lat, currentWeather.Location.Lon);
            var isDay = DateTime.Now > calc.DSunRise && DateTime.Now < calc.DSunSet;
            if (isDay)
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_cloudy_day.mp4");
            else
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_cloudy_night.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }

        private void StartRainAnim()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_rain.mp4") || !File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_rain_night.mp4"))
                return;
            var calc = new SunCalculator(DateTime.Now, currentWeather.Location.Lat, currentWeather.Location.Lon);
            var isDay = DateTime.Now > calc.DSunRise && DateTime.Now < calc.DSunSet;
            if (isDay)
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_rain.mp4");
            else
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_rain_night.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }

        private void StartLightningAnim()
        {
            if (!File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_thunderstorm_day.mp4") || !File.Exists(E.Root + "\\Extras\\WeatherAnimation\\weather_thunderstorm_night.mp4"))
                return;
            var calc = new SunCalculator(DateTime.Now, currentWeather.Location.Lat, currentWeather.Location.Lon);
            var isDay = DateTime.Now > calc.DSunRise && DateTime.Now < calc.DSunSet;
            if (isDay)
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_thunderstorm_day.mp4");
            else
                MediaElement.Source = new Uri(E.Root + "\\Extras\\WeatherAnimation\\weather_thunderstorm_night.mp4");
            var s = (Storyboard)Resources["ShowVideoAnim"];
            s.Begin();
            MediaElement.Play();
        }

        private void MediaElementMediaEnded(object sender, RoutedEventArgs e)
        {
            MediaElement.Position = new TimeSpan();
            var s = (Storyboard)Resources["HideVideoAnim"];
            s.Begin();
        }

        private void DoubleAnimationCompleted(object sender, EventArgs e)
        {
            MediaElement.Stop();
            MediaElement.Close();
        }

        private void ClearItem_Click(object sender, RoutedEventArgs e)
        {
            StartClearAnim();
        }

        private void PinItemClick(object sender, RoutedEventArgs e)
        {
            Globals.Settings.Pin = PinItem.IsChecked;
            Globals.Settings.Save(Globals.ConfigFile);
        }

        private void MouseEnterCompleted(object sender, EventArgs e)
        {
            this.Opacity = 1;
        }

        private void MouseLeaveCompleted(object sender, EventArgs e)
        {
            this.Opacity = Globals.Settings.Opacity;
        }

        private void ThisMouseEnter(object sender, MouseEventArgs e)
        {
            var mouseEnterAnim = (Storyboard)Resources["MouseEnter"];
            mouseEnterAnim.Begin(this);
        }

        private void ThisMouseLeave(object sender, MouseEventArgs e)
        {
            var mouseLeaveAnim = (Storyboard)Resources["MouseLeave"];
            ((DoubleAnimation)mouseLeaveAnim.Children[0]).To = Globals.Settings.Opacity;
            mouseLeaveAnim.Begin(this);
        }

        private void RainItemClick(object sender, RoutedEventArgs e)
        {
            WeatherIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/weather_12.png", UriKind.Relative));
        }

        private void LightningItemClick(object sender, RoutedEventArgs e)
        {
            WeatherIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/weather_15.png", UriKind.Relative));
        }
    }
}
