using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Windows.Threading;
using Clock.Controls;
using Home.Base;
using Microsoft.Win32;
using Weather.Base;
using Path = System.IO.Path;
using Home.Packaging;

namespace Clock.Windows
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class FlipClockOptions : Window
    {
        private DispatcherTimer searchDelayTimer;
        private List<LocationData> locations;
        private LocationData currentLocation;
        private readonly string[] dateFormats = new string[] { "M/d/yyyy", "d/M/yyyy", "yyyy/M/d", "ddd, MMM d", "ddd, d MMMM", "dddd, MMM d", "dddd, d MMMM" };
        private readonly List<string> langCodes = new List<string>();
        private bool restartRequired;

        public event EventHandler UpdateSettings;

        public FlipClockOptions()
        {
            InitializeComponent();

            SearchPopup.MouseDown += (s, e) => e.Handled = true;
        }

        private void WindowSourceInitialized(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;

            if (!Dwm.IsGlassAvailable() || !Dwm.IsGlassEnabled())
            {
                this.Background = new SolidColorBrush(Color.FromRgb(185, 209, 234));
            }

            double dpiY = 1.0f;
            var source = PresentationSource.FromVisual(this);

            if (source != null)
            {
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            var margins = new Home.Base.WinAPI.Margins { cyTopHeight = (int)(34 * dpiY) };

            HwndSource.FromHwnd(handle).CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

            Home.Base.Dwm.ExtendGlassFrame(handle, ref margins);

            var fileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            WidgetBuildTag.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString() + "." + fileInfo.LastWriteTimeUtc.ToString("yyMMdd-HHmm");
            HomeBuildTag.Text = Home.Base.E.VersionString;

            //fileInfo = new FileInfo(E.Root + "\\Home.Base.dll");
            //ExpirationDateTextBlock.Text = fileInfo.LastWriteTimeUtc.AddMonths(3).ToString("dd-MM-yy");

            searchDelayTimer = new DispatcherTimer();
            searchDelayTimer.Interval = TimeSpan.FromMilliseconds(1500);
            searchDelayTimer.Tick += new EventHandler(SearchDelayTimerTick);

            SearchBox.TextChanged += SearchBoxTextChanged;

            ShowWeatherCheckBox.IsChecked = App.Settings.ShowWeather;
            ShowWeatherAnimCheckBox.IsChecked = App.Settings.ShowWeatherAnimation;
            ShowForecastCheckBox.IsChecked = App.Settings.ShowForecast;
            ShowHighLowCheckBox.IsChecked = App.Settings.ShowHighLow;
            ShowFeelsLikeCheckBox.IsChecked = App.Settings.ShowFeelsLike;
            ShowAdditionalInfoCheckBox.IsChecked = App.Settings.ShowHumidity || App.Settings.ShowWindSpeed;
            ShowHumidityRadioButton.IsChecked = App.Settings.ShowHumidity;
            ShowWindSpeedRadioButton.IsChecked = App.Settings.ShowWindSpeed;

            if (App.WpManager != null && App.WpManager.Providers != null)
            {
                foreach (var p in App.WpManager.Providers)
                {
                    WeatherProvidersBox.Items.Add(p.Name);
                }
                WeatherProvidersBox.SelectedIndex = App.WpManager.Providers.IndexOf(App.WpManager.CurrentProvider);
            }

            WeatherIntervalSlider.Value = App.Settings.RefreshInterval;

            if (App.Settings.TempScale == TemperatureScale.Celsius)
                CelsiusRadioButton.IsChecked = true;

            switch (App.Settings.WindSpeedScale)
            {
                case WindSpeedScale.Mph:
                    MphRadioButton.IsChecked = true;
                    break;
                case WindSpeedScale.Kmh:
                    KmhRadioButton.IsChecked = true;
                    break;
                case WindSpeedScale.Ms:
                    MsRadioButton.IsChecked = true;
                    break;
            }

            NonSystemTimeCheckBox.IsChecked = App.Settings.UseCustomTime;

            foreach (var timeZone in TimeZoneInfo.GetSystemTimeZones())
            {
                TimeZoneComboBox.Items.Add(new ComboBoxItem() { Content = timeZone.DisplayName });
                if (timeZone.Id == App.Settings.FirstTimeZoneId)
                    TimeZoneComboBox.Text = timeZone.DisplayName;
            }
            if (!App.Settings.UseCustomTime)
                TimeZoneComboBox.Text = TimeZoneInfo.Local.DisplayName;

            foreach (var s in dateFormats)
                DateFormatComboBox.Items.Add(new ComboBoxItem() { Content = DateTime.Now.ToString(s) });
            DateFormatComboBox.Text = DateTime.Now.ToString(App.Settings.DateFormat);

            if (App.Settings.TimeFormat == 1)
                Time24HRadioButton.IsChecked = true;

            LanguageComboBox.Items.Add(new ComboBoxItem() { Content = CultureInfo.GetCultureInfo("en-US").NativeName });
            langCodes.Add("en-US");
            var langs = from x in Directory.GetDirectories(E.Root) where x.Contains("-") select Path.GetFileNameWithoutExtension(x);
            foreach (var l in langs)
            {
                try
                {
                    var c = CultureInfo.GetCultureInfo(l);
                    langCodes.Add(c.Name);
                    LanguageComboBox.Items.Add(new ComboBoxItem() { Content = c.NativeName });
                }
                catch { }
            }

            LanguageComboBox.Text = CultureInfo.GetCultureInfo(App.Settings.Language).NativeName;

            switch (App.Settings.Style)
            {
                case Styles.FlipClock:
                    FlipClockStyle.IsChecked = true;
                    break;
                case Styles.AnalogClockBig:
                    AnalogClockBigStyle.IsChecked = true;
                    break;
                case Styles.AnalogClockSmall:
                    AnalogClockSmallStyle.IsChecked = true;
                    break;
                case Styles.DualClock:
                    DualClockStyle.IsChecked = true;
                    break;
                case Styles.ModernFlipClock:
                    ModernFlipClockStyle.IsChecked = true;
                    break;
                case Styles.WideFlipClock:
                    WideFlipClockStyle.IsChecked = true;
                    break;
                case Styles.FlipClockNoWeather:
                    WeatherTab.Visibility = System.Windows.Visibility.Collapsed;
                    NoWeatherFlipClockStyle.IsChecked = true;
                    break;
            }

            SizeSlider.Value = Math.Round(App.Settings.Scale * 100);
            SizeValueTextBlock.Text = SizeSlider.Value + "%";

            TransparencySlider.Value = Math.Round((1 - App.Settings.Opacity) * 100);
            TransparencyValueTextBlock.Text = TransparencySlider.Value + "%";

            UpdateFreqSlider.Value = App.Settings.UpdateInterval;
            UpdateFreqValueTextBlock.Text = UpdateFreqSlider.Value + " " + Properties.Resources.OptionsIntervalMinutes;

            UseAeroCheckBox.IsChecked = App.Settings.UseAero;

            ShowTaskbarIconCheckBox.IsChecked = !App.Settings.UseTrayIcon;
            if (Environment.OSVersion.Version.Major <= 6 && Environment.OSVersion.Version.Minor < 1)
                ShowTaskbarIconCheckBox.Visibility = System.Windows.Visibility.Collapsed;

            var extrasList = ExtrasManager.GetInstalledExtrasInfo();
            if (extrasList != null)
            {
                foreach (var item in extrasList)
                {
                    ExtrasList.Items.Add(item);
                }
            }

            UpdatesCheckBox.IsChecked = App.Settings.CheckForUpdates;
            AutostartCheckBox.IsChecked = App.Settings.Autostart;
            SilentUpdateCheckBox.IsChecked = App.Settings.SilentUpdate;

            if (App.SoundPlayer == null)
                PlaySoundsAnimCheckBox.Visibility = System.Windows.Visibility.Collapsed;
            PlaySoundsAnimCheckBox.IsChecked = App.Settings.EnableSounds;

            ApplyButton.IsEnabled = false;
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            if (ApplyButton.IsEnabled)
                ApplySettings();
            this.Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ApplyButtonClick(object sender, RoutedEventArgs e)
        {
            ApplySettings();
            ApplyButton.IsEnabled = false;
        }

        private void CheckBoxClick(object sender, RoutedEventArgs e)
        {
            ApplyButton.IsEnabled = true;
        }

        private void ApplySettings()
        {
            if (currentLocation != null)
            {
                App.Settings.LocationCode = currentLocation.Code;
            }
            App.Settings.ShowWeather = (bool)ShowWeatherCheckBox.IsChecked;
            App.Settings.ShowForecast = (bool)ShowForecastCheckBox.IsChecked;
            App.Settings.ShowHighLow = (bool)ShowHighLowCheckBox.IsChecked;
            App.Settings.ShowFeelsLike = (bool)ShowFeelsLikeCheckBox.IsChecked;
            App.Settings.ShowHumidity = ((bool)ShowAdditionalInfoCheckBox.IsChecked && (bool)ShowHumidityRadioButton.IsChecked);
            App.Settings.ShowWindSpeed = ((bool)ShowAdditionalInfoCheckBox.IsChecked && (bool)ShowWindSpeedRadioButton.IsChecked);
            App.Settings.RefreshInterval = WeatherIntervalSlider.Value;
            App.Settings.TempScale = (bool)CelsiusRadioButton.IsChecked ? TemperatureScale.Celsius : TemperatureScale.Fahrenheit;
            App.Settings.UseCustomTime = (bool)NonSystemTimeCheckBox.IsChecked;
            App.Settings.Scale = SizeSlider.Value / 100;
            App.Settings.Opacity = 1 - (TransparencySlider.Value / 100);
            App.Settings.UseAero = (bool)UseAeroCheckBox.IsChecked;
            App.Settings.UseTrayIcon = !(bool)ShowTaskbarIconCheckBox.IsChecked;
            App.Settings.ShowWeatherAnimation = (bool)ShowWeatherAnimCheckBox.IsChecked;
            App.Settings.CheckForUpdates = (bool)UpdatesCheckBox.IsChecked;
            App.Settings.SilentUpdate = (bool)SilentUpdateCheckBox.IsChecked;
            App.Settings.EnableSounds = (bool)PlaySoundsAnimCheckBox.IsChecked;
            if (!string.IsNullOrEmpty(WeatherProvidersBox.Text))
                App.Settings.Provider = WeatherProvidersBox.Text;

            //if (App.Settings.UpdateInterval != UpdateFreqSlider.Value)
            //{
            //    App.UpdateTimer.Interval = TimeSpan.FromMinutes(UpdateFreqSlider.Value);
            //    App.UpdateTimer.Stop();
            //    App.UpdateTimer.Start();
            //}

            //if (!App.Settings.CheckForUpdates)
            //{
            //    App.UpdateTimer.Stop();
            //}

            App.Settings.UpdateInterval = (int)UpdateFreqSlider.Value;

            //if (App.Settings.UseTrayIcon)
            //    ((App)Application.Current).AddTrayIcon();
            //else
            //    ((App)Application.Current).RemoveTrayIcon();

            var lastStyle = App.Settings.Style;
            if ((bool)FlipClockStyle.IsChecked)
                App.Settings.Style = Styles.FlipClock;
            if ((bool)AnalogClockBigStyle.IsChecked)
                App.Settings.Style = Styles.AnalogClockBig;
            if ((bool)AnalogClockSmallStyle.IsChecked)
                App.Settings.Style = Styles.AnalogClockSmall;
            if ((bool)DualClockStyle.IsChecked)
                App.Settings.Style = Styles.DualClock;
            if ((bool)ModernFlipClockStyle.IsChecked)
            {
                App.Settings.Style = Styles.ModernFlipClock;
                if (!string.IsNullOrEmpty(App.Settings.LocationCode))
                    App.Settings.ShowForecast = true;
                else
                    App.Settings.ShowForecast = false;
            }
            if ((bool)WideFlipClockStyle.IsChecked)
            {
                App.Settings.Style = Styles.WideFlipClock;
                if (!string.IsNullOrEmpty(App.Settings.LocationCode))
                    App.Settings.ShowForecast = true;
                else
                    App.Settings.ShowForecast = false;
            }
            if ((bool)NoWeatherFlipClockStyle.IsChecked)
                App.Settings.Style = Styles.FlipClockNoWeather;

            restartRequired = lastStyle != App.Settings.Style;

            if (App.WpManager != null && App.WpManager.Providers != null)
                foreach (WeatherProvider p in App.WpManager.Providers)
                {
                    if (p.Name == App.Settings.Provider)
                    {
                        if (!p.IsLoaded)
                            p.Load();
                        App.WpManager.CurrentProvider = p;
                    }
                }

            var lastLang = App.Settings.Language;
            if (LanguageComboBox.SelectedIndex >= 0)
                App.Settings.Language = langCodes[LanguageComboBox.SelectedIndex];
            if (!restartRequired)
                restartRequired = lastLang != App.Settings.Language;

            if (DateFormatComboBox.SelectedIndex >= 0)
                App.Settings.DateFormat = dateFormats[DateFormatComboBox.SelectedIndex];

            if (MphRadioButton.IsChecked == true)
                App.Settings.WindSpeedScale = WindSpeedScale.Mph;
            else if (KmhRadioButton.IsChecked == true)
                App.Settings.WindSpeedScale = WindSpeedScale.Kmh;
            else
                App.Settings.WindSpeedScale = WindSpeedScale.Ms;

            if (App.Settings.UseCustomTime && TimeZoneComboBox.SelectedIndex >= 0)
            {
                var timeZone = TimeZoneInfo.GetSystemTimeZones()[TimeZoneComboBox.SelectedIndex];
                App.Settings.FirstTimeZoneId = timeZone.Id;
            }

            if (Time24HRadioButton.IsChecked == true)
                App.Settings.TimeFormat = 1;
            else
                App.Settings.TimeFormat = 0;

            if (App.Settings.Autostart != (bool)AutostartCheckBox.IsChecked)
            {
                App.Settings.Autostart = (bool)AutostartCheckBox.IsChecked;
                if (App.Settings.Autostart)
                {
                    try
                    {
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", RegistryKeyPermissionCheck.ReadWriteSubTree).OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Run", true))
                        {
                            key.SetValue("Clock Widget (HTC Home)", "\"" + Assembly.GetExecutingAssembly().Location + "\"", RegistryValueKind.String);
                            key.Close();
                        }
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", RegistryKeyPermissionCheck.ReadWriteSubTree).OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Run", true))
                        {
                            key.DeleteValue("Clock Widget (HTC Home)", false);
                            key.Close();
                        }
                    }
                    catch { }
                }
            }

            App.Settings.Save(App.ConfigFile);


            if (UpdateSettings != null)
            {
                UpdateSettings(currentLocation, EventArgs.Empty);
            }
        }

        private void SearchBoxIsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
            {
                if (string.IsNullOrEmpty(SearchBox.Text))
                {
                    SearchBox.Text = Properties.Resources.OptionsSearchBox;
                    SearchBox.FontStyle = FontStyles.Italic;
                    SearchBox.Foreground = Brushes.Gray;
                }
            }
            else
            {
                if (SearchBox.Text == Properties.Resources.OptionsSearchBox)
                {
                    SearchBox.Text = "";
                    SearchBox.FontStyle = FontStyles.Normal;
                    SearchBox.Foreground = Brushes.Black;
                }
            }
        }

        private void SearchDelayTimerTick(object sender, EventArgs e)
        {
            searchDelayTimer.Stop();
            var query = SearchBox.Text;
            ThreadStart threadStarter =
                () => GetLocations(query);
            var thread = new Thread(threadStarter);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void SearchBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (SearchBox.Foreground == Brushes.Gray)
            {
                SearchBox.Text = "";
                SearchBox.FontStyle = FontStyles.Normal;
                SearchBox.Foreground = Brushes.Black;
            }

            if (e.Key == Key.Enter)
            {
                SearchBoxTextChanged(sender, null);
            }
            //    if (e.Key == Key.Enter && !string.IsNullOrEmpty(SearchBox.Text))
            //    {
            //        SearchPopup.IsOpen = true;
            //        SearchResultBox.Items.Clear();
            //        var query = SearchBox.Text;
            //        SearchProgress.Visibility = System.Windows.Visibility.Visible;
            //        ThreadStart threadStarter =
            //            () =>
            //            {
            //                GetLocations(query);
            //            };
            //        var thread = new Thread(threadStarter);
            //        thread.SetApartmentState(ApartmentState.STA);
            //        thread.Start();
            //    }
        }

        private void GetLocations(string query)
        {
            locations = App.WpManager.CurrentProvider.GetLocations(query, CultureInfo.GetCultureInfo(App.Settings.Language), App.Settings.TempScale);
            if (locations != null && locations.Count > 0)
            {
                foreach (var location in locations)
                {
                    SearchResultBox.Dispatcher.Invoke((Action)delegate
                    {
                        //var item = new OptionsLocationItem();
                        //item.City = location.City;
                        //item.Country = location.Country;
                        //item.MouseLeftButtonDown += ItemMouseLeftButtonDown;
                        location.CityText = location.City;
                        location.CountryText = location.Country;
                        if (location.Skycode != 0 && location.Temperature != 0)
                        {
                            location.TemperatureText = location.Temperature + "°";
                            location.Icon = new BitmapImage(new Uri(string.Format("/UIFramework.Weather;Component/Images/weather_{0}.png", location.Skycode), UriKind.Relative));
                        }
                        SearchResultBox.Items.Add(location);
                    });
                }
            }
            else
            {
                SearchPopup.Dispatcher.Invoke((Action)delegate
                                                              {
                                                                  SearchPopup.IsOpen = false;
                                                              });
                searchDelayTimer.Stop();
            }
            SearchProgress.Dispatcher.Invoke((Action)(() => { SearchProgress.Visibility = System.Windows.Visibility.Collapsed; }));
        }

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchDelayTimer == null || SearchBox.Text == Properties.Resources.OptionsSearchBox)
                return;

            if (!string.IsNullOrEmpty(SearchBox.Text) && SearchBox.Text.Length > 2)
            {
                SearchPopup.IsOpen = true;
                searchDelayTimer.Stop();
                searchDelayTimer.Start();
                SearchResultBox.Items.Clear();
                SearchProgress.Visibility = System.Windows.Visibility.Visible;
                SearchButton.Source = new BitmapImage(new Uri("/Resources/FlipClock/SearchCancelIcon.png", UriKind.Relative));
            }
            else
            {
                SearchPopup.IsOpen = false;
                SearchResultBox.Items.Clear();
                SearchProgress.Visibility = System.Windows.Visibility.Collapsed;
                searchDelayTimer.Stop();
                SearchButton.Source = new BitmapImage(new Uri("/Resources/FlipClock/SearchIcon.png", UriKind.Relative));
            }
        }

        private void SearchResultBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultBox.SelectedIndex == -1)
                return;
            if (SearchResultBox.SelectedIndex > locations.Count)
                currentLocation = locations[locations.Count - 1];
            else
                currentLocation = locations[SearchResultBox.SelectedIndex];
            SearchBox.Text = currentLocation.City;
            searchDelayTimer.Stop();
            SearchPopup.IsOpen = false;
            SearchResultBox.Items.Clear();
            ApplyButton.IsEnabled = true;
        }

        private void WeatherIntervalSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyButton.IsEnabled = true;
            if (WeatherIntervalSlider.Value < 60)
            {
                WeatherIntervalValueTextBlock.Text = WeatherIntervalSlider.Value + " " + Properties.Resources.OptionsIntervalMinutes;
            }
            else if (WeatherIntervalSlider.Value == 60)
            {
                WeatherIntervalValueTextBlock.Text = 1 + " " + Properties.Resources.OptionsIntervalHours;
            }
            else
            {
                WeatherIntervalValueTextBlock.Text = string.Format("{0} {1} {2} {3}", Math.Truncate(WeatherIntervalSlider.Value / 60), Properties.Resources.OptionsIntervalHours,
                    Math.Abs(Math.IEEERemainder(WeatherIntervalSlider.Value, 60)), Properties.Resources.OptionsIntervalMinutes);
            }
        }

        private void SearchButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SearchBox.Text = "";
            SearchBox.Focus();
            SearchButton.Source = new BitmapImage(new Uri("/Resources/FlipClock/SearchIcon.png", UriKind.Relative));
            e.Handled = true;
        }

        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyButton.IsEnabled = true;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            App.Settings.OptionsWidth = Width;
            App.Settings.OptionsHeight = Height;
            if (restartRequired)
            {
                Process.Start(Application.ResourceAssembly.Location, "/c \"" + App.ConfigFile + "\"");
                Application.Current.Shutdown();
            }
        }

        private void SizeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyButton.IsEnabled = true;
            SizeValueTextBlock.Text = SizeSlider.Value + "%";
        }

        private void TransparencySliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyButton.IsEnabled = true;
            TransparencyValueTextBlock.Text = TransparencySlider.Value + "%";
        }

        private void TabItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(ShowWeatherCheckBox); //remove focus from search box
        }

        private void ExtrasListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && ((ExtrasInfo)e.AddedItems[0]).Removable)
                DeleteExtButton.IsEnabled = true;
            else
                DeleteExtButton.IsEnabled = false;
        }

        private void DeleteExtButtonClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.OptionsExtrasDeleteMessage, "HTC Home", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
            {
                ExtrasManager.DeleteExtra((ExtrasInfo)ExtrasList.SelectedItem);
                ExtrasList.Items.Remove(ExtrasList.SelectedItem);
            }
        }

        private void SiteLinkMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WinAPI.ShellExecute(IntPtr.Zero, "open", "http://htchome.org", string.Empty, string.Empty, 0);
        }

        private void InstallExtButtonClick(object sender, RoutedEventArgs e)
        {
            var d = new System.Windows.Forms.OpenFileDialog();
            d.DefaultExt = "*.hhpack";
            d.Filter = "HTC Home Package (*.hhpack)|*.hhpack|All files (*.*)|*.*";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var packageManager = new PackageManager();
                packageManager.BeginUnpack(d.FileName, E.Root);
            }
        }

        private void UpdateFreqSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyButton.IsEnabled = true;
            UpdateFreqValueTextBlock.Text = UpdateFreqSlider.Value + " " + Properties.Resources.OptionsIntervalMinutes;
        }

        private void CheckUpdatesButtonClick(object sender, RoutedEventArgs e)
        {
            CheckUpdatesButton.IsEnabled = false;

            var args = App.Settings.Language + " /wcheck";
            Process.Start(E.Root + "\\Update.exe", args);
        }
    }
}
