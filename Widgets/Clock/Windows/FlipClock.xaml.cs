using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Clock.Controls;
using Clock.Domain;
using Clock.WeatherAnimation;
using Home.Base;
using Weather.Base;

namespace Clock.Windows
{
    /// <summary>
    /// Interaction logic for FlipClock.xaml
    /// </summary>
    public partial class FlipClock : Window
    {
        private IntPtr handle;
        private DispatcherTimer clockTimer;
        private DispatcherTimer weatherTimer;
        private FlipClockOptions optionsWindow;
        private WeatherData currentWeather;
        private LocationData currentLocation;
        private TimeSpan baseUtcOffset;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public FlipClock()
        {
            InitializeComponent();
        }

        private void WindowSourceInitialized(object sender, EventArgs e)
        {
            handle = new WindowInteropHelper(this).Handle;

            if (!App.Settings.DisableUnminimizer)
            {
                var u = new Unminimizer();
                u.Initialize(handle);
            }

            Dwm.RemoveFromAeroPeek(handle);
            Dwm.RemoveFromAltTab(handle);
            Dwm.RemoveFromFlip3D(handle);

            this.Left = App.Settings.Left;
            this.Top = App.Settings.Top;

            if (this.Left == -100.0f || this.Top == -100.0f)
            {
                this.Left = SystemParameters.WorkArea.Width / 2 - this.Width / 2;
                this.Top = SystemParameters.WorkArea.Height / 2 - this.Height / 2;
            }

            this.Opacity = App.Settings.Opacity;
            
            currentLocation = new LocationData();
            currentLocation.Code = App.Settings.LocationCode;

            currentWeather = (WeatherData)XmlSerializable.Load(typeof(WeatherData), E.Root + "\\Clock.Weather.data") ?? new WeatherData();
            //lastWeatherState = WeatherConverter.ConvertSkyCodeToWeatherState(currentWeather.Curent.SkyCode);

            if (App.Settings.ShowForecast && App.Settings.ShowWeather)
            {
                ForecastGrid.Opacity = 1;
                ForecastGrid.Height = 70;
                this.Height = 280;
                WeatherIcon.ToolTip = Properties.Resources.HideForecastTooltip;
                ForecastItem.IsChecked = true;
            }

            if (App.Settings.UseCustomTime)
            {
                baseUtcOffset = TimeZoneInfo.FindSystemTimeZoneById(App.Settings.FirstTimeZoneId).GetUtcOffset(DateTime.Now);
            }
            else
            {
                baseUtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            }

            var h = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).AddHours(-1).Hour;
            if (App.Settings.TimeFormat == 0)
            {
                h = Convert.ToInt32(DateTime.Now.ToUniversalTime().Add(baseUtcOffset).AddHours(-1).ToString("hh"));
            }

            HoursTab.Value = h;
            MinutesTab.Value = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).AddMinutes(-2).Minute;

            HoursTab.FlipCompleted += HoursTabFirstFlipCompleted;
            MinutesTab.FlipCompleted += MinutesTabFirstFlipCompleted;
            h = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).Hour;
            if (App.Settings.TimeFormat == 0)
            {
                HoursTab.TimeMode = h < 12 ? TimeMode.Am : TimeMode.Pm;
                h = Convert.ToInt32(DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString("hh"));
            }
            HoursTab.Flip(h);
            MinutesTab.Flip(DateTime.Now.ToUniversalTime().Add(baseUtcOffset).AddMinutes(-1).Minute);

            DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);

            this.ShowInTaskbar = !App.Settings.UseTrayIcon;

            if (App.Settings.TopMost)
            {
                this.Topmost = true;
                TopMostItem.IsChecked = true;
            }

            if (App.Settings.Pin)
                PinItem.IsChecked = true;

            if (App.WpManager.CurrentProvider == null)
            {
                App.Settings.Provider = "MSN";
                App.WpManager.CurrentProvider = App.WpManager.Providers.Find(p => p.Name == "MSN");
                App.WpManager.CurrentProvider.Load();
            }

            if (currentLocation.Code != null)
                RefreshWeather();

            if (App.Settings.DisableShadows)
            {
                WeatherStackPanel.Effect = null;
                RightGrid.Effect = null;
                WindSpeedValueTextBlock.Effect = null;
                HumidityValueTextBlock.Effect = null;
            }

        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Activate();

            clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += ClockTimerTick;
            //clockTimer starts when MinutesTab first flip completed

            UpdateWeatherUI();

            if (App.Settings.ShowWeather)
            {
                if (string.IsNullOrEmpty(currentLocation.Code))
                {
                    WeatherGrid.Visibility = System.Windows.Visibility.Collapsed;
                    SetupLocationTextBlock.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    WeatherGrid.Visibility = System.Windows.Visibility.Visible;
                    SetupLocationTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                WeatherGrid.Visibility = System.Windows.Visibility.Collapsed;
                SetupLocationTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            }

            weatherTimer = new DispatcherTimer();
            weatherTimer.Interval = TimeSpan.FromMinutes(App.Settings.RefreshInterval);
            weatherTimer.Tick += WeatherTimerTick;
            weatherTimer.Start();

            Scale.ScaleX = App.Settings.Scale;

            if (App.Settings.UseAero)
            {
                UpdateAeroGlass();
            }
        }

        private void UpdateAeroGlass()
        {
            double dpiX = 0.0f;
            double dpiY = 0.0f;
            var source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            var rgn = WinAPI.CreateRoundRectRgn((int)(100 * App.Settings.Scale * dpiX), (int)(15 * App.Settings.Scale * dpiY),
                (int)((this.Width - 100) * App.Settings.Scale * dpiX), (int)(this.Height * App.Settings.Scale * dpiY),
                (int)(15 * App.Settings.Scale * dpiX), (int)(dpiY * App.Settings.Scale * 15));
            Dwm.MakeGlassRegion(ref handle, rgn);
        }

        private void UpdateAeroGlass(double height)
        {
            double dpiX = 0.0f;
            double dpiY = 0.0f;
            var source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            var rgn = WinAPI.CreateRoundRectRgn((int)(100 * App.Settings.Scale * dpiX), (int)(15 * App.Settings.Scale * dpiY),
                (int)((this.Width - 100) * App.Settings.Scale * dpiX), (int)(height * App.Settings.Scale * dpiY),
                (int)(15 * App.Settings.Scale * dpiX), (int)(dpiY * App.Settings.Scale * 15));
            Dwm.MakeGlassRegion(ref handle, rgn);
        }

        void WeatherTimerTick(object sender, EventArgs e)
        {
            RefreshWeather();
        }

        private void RefreshWeather()
        {
            WeatherRefreshProgressBar.Visibility = System.Windows.Visibility.Visible;
            Taskbar.ProgressState = TaskbarItemProgressState.Indeterminate;

            ThreadStart threadStarter = () =>
                                            {
                                                logger.Info("Getting weather report");
                                                var w = App.WpManager.CurrentProvider.GetWeatherReport(
                                                        CultureInfo.GetCultureInfo(App.Settings.Language), currentLocation,
                                                        App.Settings.TempScale, App.Settings.WindSpeedScale, baseUtcOffset);
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
                                                this.Dispatcher.Invoke((Action)delegate
                                                                                    {
                                                                                        Taskbar.ProgressState = TaskbarItemProgressState.None;
                                                                                    });
                                                currentWeather.Save(E.Root + "\\Clock.Weather.data");
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

                                                          if (App.Settings.ShowFeelsLike)
                                                              TempTextBlock.Text = currentWeather.FeelsLike + "°";
                                                          else
                                                              TempTextBlock.Text = currentWeather.Temperature + "°";

                                                          if (currentWeather.ForecastList.Count > 0)
                                                          {
                                                              HighTempTextBlock.Text = currentWeather.ForecastList[0].HighTemperature + "°";
                                                              LowTempTextBlock.Text = currentWeather.ForecastList[0].LowTemperature + "°";
                                                          }

                                                          WeatherIcon.Source = new BitmapImage(new Uri(string.Format("/UIFramework.Weather;Component/Images/weather_{0}.png",
                                                                                   currentWeather.Curent.SkyCode), UriKind.Relative));

                                                          if (!string.IsNullOrEmpty(currentWeather.Curent.Text))
                                                              this.Title = currentWeather.Curent.Text;
                                                          if (App.Settings.ShowHighLow)
                                                          {
                                                              HighTempTextBlock.Visibility = System.Windows.Visibility.Visible;
                                                              HighTempValueTextBlock.Visibility = System.Windows.Visibility.Visible;
                                                              LowTempTextBlock.Visibility = System.Windows.Visibility.Visible;
                                                              LowTempValueTextBlock.Visibility = System.Windows.Visibility.Visible;
                                                          }
                                                          else
                                                          {
                                                              HighTempTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                                                              HighTempValueTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                                                              LowTempTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                                                              LowTempValueTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                                                          }

                                                          if (App.Settings.ShowWindSpeed)
                                                          {
                                                              HumidityPanel.Visibility = System.Windows.Visibility.Collapsed;
                                                              WindSpeedPanel.Visibility = System.Windows.Visibility.Visible;
                                                              switch (App.Settings.WindSpeedScale)
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
                                                              var windSpeedKmh = (int)WeatherConverter.WindSpeedConvertToKmh(currentWeather.WindSpeed, App.Settings.WindSpeedScale);

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

                                                              WindSpeedValueTextBlock.ToolTip = Properties.Resources.WindSpeed + " " + WindSpeedValueTextBlock.Text + "\n" +
                                                                  Properties.Resources.Humidity + " " + currentWeather.Humidity + "%";
                                                              WindSpeedIcon.ToolTip = WindSpeedValueTextBlock.ToolTip;
                                                          }
                                                          else
                                                          {
                                                              WindSpeedPanel.Visibility = System.Windows.Visibility.Collapsed;
                                                          }

                                                          if (App.Settings.ShowHumidity)
                                                          {
                                                              WindSpeedPanel.Visibility = System.Windows.Visibility.Collapsed;
                                                              HumidityPanel.Visibility = System.Windows.Visibility.Visible;
                                                              HumidityValueTextBlock.Text = currentWeather.Humidity + "%";

                                                              if (currentWeather.Humidity < 25)
                                                                  HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl1.png", UriKind.Relative));
                                                              if (currentWeather.Humidity >= 25 && currentWeather.Humidity < 50)
                                                                  HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl2.png", UriKind.Relative));
                                                              if (currentWeather.Humidity >= 50 && currentWeather.Humidity < 75)
                                                                  HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl3.png", UriKind.Relative));
                                                              if (currentWeather.Humidity >= 75)
                                                                  HumidityIcon.Source = new BitmapImage(new Uri("/UIFramework.Weather;Component/Images/humidity_lvl4.png", UriKind.Relative));

                                                              HumidityValueTextBlock.ToolTip = Properties.Resources.Humidity + " " + HumidityValueTextBlock.Text + "\n" + 
                                                                  Properties.Resources.WindSpeed + " " + currentWeather.WindSpeed + " ";

                                                              switch (App.Settings.WindSpeedScale)
                                                              {
                                                                  case WindSpeedScale.Mph:
                                                                      HumidityValueTextBlock.ToolTip += Properties.Resources.Mph;
                                                                      break;
                                                                  case WindSpeedScale.Kmh:
                                                                      HumidityValueTextBlock.ToolTip += Properties.Resources.Kmh;
                                                                      break;
                                                                  case WindSpeedScale.Ms:
                                                                      HumidityValueTextBlock.ToolTip += Properties.Resources.Ms;
                                                                      break;
                                                              }

                                                              HumidityIcon.ToolTip = HumidityValueTextBlock.ToolTip;
                                                          }
                                                          else
                                                          {
                                                              HumidityPanel.Visibility = System.Windows.Visibility.Collapsed;
                                                          }

                                                          if ((App.Settings.ShowWindSpeed || App.Settings.ShowHumidity) && !App.Settings.ShowForecast)
                                                          {
                                                              this.Height = 210;
                                                          }
                                                          else if (!App.Settings.ShowForecast)
                                                          {
                                                              this.Height = 200;
                                                          }

                                                          if (App.Settings.UseAero)
                                                          {
                                                              UpdateAeroGlass();
                                                          }

                                                          if (!string.IsNullOrEmpty(App.Settings.LocationCode))
                                                              SetWeatherState(WeatherConverter.ConvertSkyCodeToWeatherState(currentWeather.Curent.SkyCode));
                                                          //var state = WeatherConverter.ConvertSkyCodeToWeatherState(currentWeather.Curent.SkyCode);
                                                          //if (state != lastWeatherState)
                                                          //{
                                                          //    lastWeatherState = state;
                                                          //    SetWeatherState(lastWeatherState);
                                                          //}

                                                      }, null);
            ForecastPanel.Dispatcher.Invoke((Action)delegate
                                                        {
                                                            if (currentWeather.ForecastList.Count >= 5)
                                                            {
                                                                var i = 1;
                                                                foreach (var item in ForecastPanel.Children)
                                                                {
                                                                    if (item.GetType() == typeof(ForecastItem))
                                                                    {
                                                                        var forecastItem = (ForecastItem)item;
                                                                        forecastItem.Temperature.Text = currentWeather.ForecastList[i].HighTemperature + "°/" +
                                                                            currentWeather.ForecastList[i].LowTemperature + "°";
                                                                        forecastItem.DayName.Text = DateTime.Now.AddDays(i).ToString("ddd").ToLower();
                                                                        forecastItem.FlipWeather(currentWeather.ForecastList[i].SkyCode);
                                                                        /*forecastItem.Icon.Source = new BitmapImage(new Uri(string.Format("/UIFramework.Weather;Component/Images/weather_{0}.png",
                                                                                        currentWeather.ForecastList[i].SkyCode), UriKind.Relative));*/
                                                                        forecastItem.ToolTip = currentWeather.ForecastList[i].Text + "\n" + Properties.Resources.ForecastTooltip;
                                                                        forecastItem.Url = currentWeather.ForecastList[i].Url;
                                                                        i++;
                                                                    }
                                                                }
                                                            }

                                                            ((ForecastItem)ForecastPanel.Children[0]).DayName.Text = Properties.Resources.Tomorrow;

                                                        });
            this.Dispatcher.Invoke((Action)delegate
                                                {
                                                    if (App.Settings.ShowWeather)
                                                        RefreshItem.Visibility = System.Windows.Visibility.Visible;
                                                    else
                                                        RefreshItem.Visibility = System.Windows.Visibility.Collapsed;
                                                });
        }

        void HoursTabFirstFlipCompleted(object sender, EventArgs e)
        {
            HoursTab.Delay = 0;
        }

        void MinutesTabFirstFlipCompleted(object sender, EventArgs e)
        {
            MinutesTab.Delay = 0;
            if (!App.Settings.UseCustomTime)
                MinutesTab.Flip(DateTime.Now.Minute);
            else
                MinutesTab.Flip(DateTime.Now.ToUniversalTime().Add(baseUtcOffset).Minute);

            MinutesTab.FlipCompleted -= MinutesTabFirstFlipCompleted;

            clockTimer.Start();
        }

        private void WindowMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !App.Settings.Pin)
            {
                DragMove();
            }
        }

        private void ClockTimerTick(object sender, EventArgs e)
        {
            if (!App.Settings.UseCustomTime)
            {
                if (MinutesTab.Value != DateTime.Now.Minute)
                {
                    MinutesTab.Flip(DateTime.Now.Minute);
                    DateTextBlock.Text = DateTime.Now.ToString(App.Settings.DateFormat);
                }
                var h = DateTime.Now.Hour;
                if (App.Settings.TimeFormat == 0)
                {
                    HoursTab.TimeMode = h < 12 ? TimeMode.Am : TimeMode.Pm;
                    h = Convert.ToInt32(DateTime.Now.ToString("hh"));
                }
                else
                    HoursTab.TimeMode = TimeMode.None;
                if (HoursTab.Value != h)
                {
                    HoursTab.Flip(h);
                    DateTextBlock.Text = DateTime.Now.ToString(App.Settings.DateFormat);
                }
            }
            else
            {
                if (MinutesTab.Value != DateTime.Now.ToUniversalTime().Add(baseUtcOffset).Minute)
                {
                    MinutesTab.Flip(DateTime.Now.ToUniversalTime().Add(baseUtcOffset).Minute);
                    DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);
                }
                var h = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).Hour;
                if (App.Settings.TimeFormat == 0)
                {
                    HoursTab.TimeMode = h < 12 ? TimeMode.Am : TimeMode.Pm;
                    h = Convert.ToInt32(DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString("hh"));
                }
                else
                    HoursTab.TimeMode = TimeMode.None;
                if (HoursTab.Value != h)
                {
                    HoursTab.Flip(h);
                    DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);
                }
            }
        }


        private void CloseItemClick(object sender, RoutedEventArgs e)
        {
            currentWeather.Save(E.Root + "\\Clock.Weather.data");
            this.Close();
        }

        private void OptionsItemClick(object sender, RoutedEventArgs e)
        {
            if (optionsWindow != null && optionsWindow.IsVisible)
            {
                optionsWindow.Activate();
                return;
            }

            optionsWindow = new FlipClockOptions();
            optionsWindow.UpdateSettings += OptionsWindowUpdateSettings;

            optionsWindow.Width = App.Settings.OptionsWidth;
            optionsWindow.Height = App.Settings.OptionsHeight;

            if (App.Settings.Language == "he-IL" || App.Settings.Language == "ar-SA")
            {
                optionsWindow.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                optionsWindow.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            }

            optionsWindow.ShowDialog();
        }

        void OptionsWindowUpdateSettings(object sender, EventArgs e)
        {
            if (sender != null)
                currentLocation = (LocationData)sender;

            if (currentLocation.Code != null)
            {
                RefreshWeather();
            }

            if (App.Settings.ShowWeather)
            {
                if (weatherTimer.Interval.Minutes != App.Settings.RefreshInterval)
                {
                    weatherTimer.Interval = TimeSpan.FromMinutes(App.Settings.RefreshInterval);
                    weatherTimer.Stop();
                    weatherTimer.Start();
                }
                if (string.IsNullOrEmpty(currentLocation.City) || string.IsNullOrEmpty(currentLocation.Code))
                {
                    WeatherGrid.Visibility = System.Windows.Visibility.Collapsed;
                    SetupLocationTextBlock.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    WeatherGrid.Visibility = System.Windows.Visibility.Visible;
                    SetupLocationTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                WeatherGrid.Visibility = System.Windows.Visibility.Collapsed;
                SetupLocationTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            }

            Scale.ScaleX = App.Settings.Scale;

            if (App.Settings.UseAero)
                UpdateAeroGlass();
            else
                Dwm.RemoveGlassRegion(ref handle);

            //if (App.Settings.ShowForecast && App.Settings.ShowWeather)
            //{
            //    var s = (Storyboard)Resources["ShowForecastAnim"];
            //    s.Stop();
            //    s = (Storyboard)Resources["HideForecastAnim"];
            //    s.Stop();
            //    ForecastGrid.Height = 70;
            //    ForecastGrid.Opacity = 1;
            //}
            //else
            //{
            //    var s = (Storyboard)Resources["ShowForecastAnim"];
            //    s.Stop();
            //    s = (Storyboard)Resources["HideForecastAnim"];
            //    s.Stop();
            //    ForecastGrid.Opacity = 0;
            //    ForecastGrid.Height = 0;
            //}

            if (App.Settings.UseCustomTime)
            {
                baseUtcOffset = TimeZoneInfo.FindSystemTimeZoneById(App.Settings.FirstTimeZoneId).GetUtcOffset(DateTime.Now);
                DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);
            }
            else
            {
                baseUtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                DateTextBlock.Text = DateTime.Now.ToString(App.Settings.DateFormat);
            }

            this.ShowInTaskbar = !App.Settings.UseTrayIcon;
            this.Opacity = App.Settings.Opacity;
        }

        private double x, y;

        private void WeatherIconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            x = this.Left;
            y = this.Top;
        }

        private void WeatherIconMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && x == this.Left && y == this.Top)
            {
                if (!App.Settings.ShowForecast)
                {
                    App.Settings.ShowForecast = true;
                    var s = (Storyboard)Resources["ShowForecastAnim"];
                    s.Begin();
                }
                else
                {
                    App.Settings.ShowForecast = false;
                    var s = (Storyboard)Resources["HideForecastAnim"];
                    if (App.Settings.ShowHumidity || App.Settings.ShowWindSpeed)
                    {
                        ((DoubleAnimation)s.Children[0]).To = 210;
                        if (App.Settings.UseAero)
                            UpdateAeroGlass(210);
                    }
                    else
                    {
                        ((DoubleAnimation)s.Children[0]).To = 200;
                        if (App.Settings.UseAero)
                            UpdateAeroGlass(200);
                    }
                    s.Begin();
                }
                ForecastItem.IsChecked = App.Settings.ShowForecast;
            }
        }

        private void WindowMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            App.Settings.Left = this.Left;
            App.Settings.Top = this.Top;
            App.Settings.Save(App.ConfigFile);
        }

        private void RefreshItemClick(object sender, RoutedEventArgs e)
        {
            RefreshWeather();
        }


        private void SnowItemClick(object sender, RoutedEventArgs e)
        {
            WeatherFxHost.Children.Clear();
            //var windSpeed = WeatherConverter.WindSpeedConvertToKmh(currentWeather.WindSpeed,
            //                                                       App.Settings.WindSpeedScale);
            //for (var i = 0; i < 30; i++)
            //{
            //    var s = new Snowflake();
            //    s.Initialize(i, i * 100, (int)windSpeed);
            //    WeatherFxHost.Children.Add(s.ParticleControl);
            //}

            /*var snowEmitter = new SnowEmitter();
            snowEmitter.MaxParticles = 1;
            snowEmitter.Height = this.Height;
            snowEmitter.Width = this.Width;
            snowEmitter.Position = new Point(0,0);
            snowEmitter.Velocity = new Vector(0,1);
            snowEmitter.LifeTime = 5;
            snowEmitter.Initialize(ref WeatherFxHost);*/

        }

        private void LightningItemClick(object sender, RoutedEventArgs e)
        {
            StartRainAnim(40);
            StartLightningAnim();
        }

        private void RainItemClick(object sender, RoutedEventArgs e)
        {
            StartRainAnim(30);
        }

        private void CloudsItemClick(object sender, RoutedEventArgs e)
        {
            StartCloudAnimation();
        }

        private void StartCloudAnimation()
        {
            WeatherFxHost.Children.Clear();
            for (int i = 0; i < 5; i++)
            {
                Cloud c = new Cloud();
                c.Initialize(i);
                WeatherFxHost.Children.Add(c);
            }
        }

        private void StartRainAnim(int raindropCount)
        {
            WeatherFxHost.Children.Clear();
            //var windSpeed = WeatherConverter.WindSpeedConvertToKmh(currentWeather.WindSpeed,
            //                                                       App.Settings.WindSpeedScale);
            //for (var i = 0; i < raindropCount; i++)
            //{
            //    var r = new RainDrop();
            //    r.Initialize(i, i * 10, (int)windSpeed);
            //    WeatherFxHost.Children.Add(r.ParticleControl);
            //}

            for (int i = 0; i < raindropCount; i++)
            {
                var r = new Raindrop();
                r.Initialize(i, 2500 + i * 100);
                WeatherFxHost.Children.Add(r);
            }

            for (int i = 0; i < 7; i++)
            {
                var r2 = new Raindrop2();
                r2.Initialize(i);
                WeatherFxHost.Children.Add(r2);
            }

            if (this.Top <= 10)
            {
                var wiper = new RainWiper();
                wiper.Initialize();
                WeatherFxHost.Children.Add(wiper);
            }

            var c1 = new RainCloud();
            c1.Initialize(0);
            var c2 = new RainCloud();
            c2.Initialize(1);
            WeatherFxHost.Children.Add(c1);
            WeatherFxHost.Children.Add(c2);
        }

        public void StartLightningAnim()
        {
            var l1 = new Lightning();
            l1.Initialize(1, 0, ref LightningBg1);
            Lightning l2 = new Lightning();
            l2.Initialize(2, 1, ref LightningBg2);
            WeatherFxHost.Children.Add(l1);
            WeatherFxHost.Children.Add(l2);
        }

        private void ShowForecastAnimCompleted(object sender, EventArgs e)
        {
            this.Height = 280;
            if (App.Settings.UseAero)
                UpdateAeroGlass();
            WeatherIcon.ToolTip = Properties.Resources.HideForecastTooltip;
        }

        private void HideForecastAnimCompleted(object sender, EventArgs e)
        {
            if (App.Settings.ShowHumidity || App.Settings.ShowWindSpeed)
                this.Height = 210;
            else
                this.Height = 200;
            if (App.Settings.UseAero)
                UpdateAeroGlass();
            WeatherIcon.ToolTip = Properties.Resources.ShowForecastTooltip;
        }

        private void TopMostItemClick(object sender, RoutedEventArgs e)
        {
            if (App.Settings.TopMost)
            {
                App.Settings.TopMost = false;
                this.Topmost = false;
            }
            else
            {
                App.Settings.TopMost = true;
                this.Topmost = true;
            }
            App.Settings.Save(App.ConfigFile);
        }

        private void SetWeatherState(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.Clouds:
                case WeatherState.PartlyCloud:
                case WeatherState.PartlySunny:
                    if (App.Settings.ShowWeatherAnimation)
                        StartCloudAnimation();
                    if (App.SoundPlayer != null && App.Settings.EnableSounds && App.Settings.ShowWeather && !string.IsNullOrEmpty(App.Settings.LocationCode))
                    {
                        App.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_clouds.wav";
                        App.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.HeavyRain:
                    if (App.Settings.ShowWeatherAnimation)
                        StartRainAnim(50);
                    if (App.SoundPlayer != null && App.Settings.EnableSounds && App.Settings.ShowWeather && !string.IsNullOrEmpty(App.Settings.LocationCode))
                    {
                        App.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_showers.wav";
                        App.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.SmallRain:
                    if (App.Settings.ShowWeatherAnimation)
                        StartRainAnim(25);
                    if (App.SoundPlayer != null && App.Settings.EnableSounds && App.Settings.ShowWeather && !string.IsNullOrEmpty(App.Settings.LocationCode))
                    {
                        App.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_showers.wav";
                        App.SoundPlayer.Play();
                    }
                    break;
                case WeatherState.Storm:
                    if (App.Settings.ShowWeatherAnimation)
                    {
                        StartRainAnim(40);
                        StartLightningAnim();
                    }

                    if (App.SoundPlayer != null && App.Settings.EnableSounds && App.Settings.ShowWeather && !string.IsNullOrEmpty(App.Settings.LocationCode))
                    {
                        App.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_thunder.wav";
                        App.SoundPlayer.Play();
                    }
                    break;

                case WeatherState.Clear:
                    if (App.SoundPlayer != null && App.Settings.EnableSounds && App.Settings.ShowWeather && !string.IsNullOrEmpty(App.Settings.LocationCode))
                    {
                        App.SoundPlayer.SoundLocation = E.ExtPath + "\\WeatherSounds\\sound_sunny.wav";
                        App.SoundPlayer.Play();
                    }
                    break;
            }
        }

        private void PinItemClick(object sender, RoutedEventArgs e)
        {
            App.Settings.Pin = PinItem.IsChecked;
            App.Settings.Save(App.ConfigFile);
        }

        private void ForecastItemClick(object sender, RoutedEventArgs e)
        {
            if (!App.Settings.ShowForecast)
            {
                App.Settings.ShowForecast = true;
                var s = (Storyboard)Resources["ShowForecastAnim"];
                s.Begin();
            }
            else
            {
                App.Settings.ShowForecast = false;
                var s = (Storyboard)Resources["HideForecastAnim"];
                if (App.Settings.ShowHumidity || App.Settings.ShowWindSpeed)
                {
                    ((DoubleAnimation)s.Children[0]).To = 210;
                    if (App.Settings.UseAero)
                        UpdateAeroGlass(210);
                }
                else
                {
                    ((DoubleAnimation)s.Children[0]).To = 200;
                    if (App.Settings.UseAero)
                        UpdateAeroGlass(200);
                }
                s.Begin();
            }

            App.Settings.Save(App.ConfigFile);
        }

        private void MouseEnterCompleted(object sender, EventArgs e)
        {
            this.Opacity = 1;
        }

        private void MouseLeaveCompleted(object sender, EventArgs e)
        {
            this.Opacity = App.Settings.Opacity;
        }

        private void ThisMouseEnter(object sender, MouseEventArgs e)
        {
            var mouseEnterAnim = (Storyboard)Resources["MouseEnter"];
            mouseEnterAnim.Begin(this);
        }

        private void ThisMouseLeave(object sender, MouseEventArgs e)
        {
            var mouseLeaveAnim = (Storyboard)Resources["MouseLeave"];
            ((DoubleAnimation)mouseLeaveAnim.Children[0]).To = App.Settings.Opacity;
            mouseLeaveAnim.Begin(this);
        }

        private void ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(App.Settings.LocationCode))
                ForecastItem.IsEnabled = false;
            else
                ForecastItem.IsEnabled = true;
        }
    }
}
