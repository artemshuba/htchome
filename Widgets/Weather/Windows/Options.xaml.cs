using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Home.Base;
using Microsoft.Win32;
using Weather.Base;
using Path = System.IO.Path;

namespace Weather.Windows
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        private DispatcherTimer searchDelayTimer;
        private List<LocationData> locations;
        private LocationData currentLocation;
        private readonly string[] dateFormats = new string[] { "M/d/yyyy", "d/M/yyyy", "yyyy/M/d", "ddd, d MMM", "ddd, d MMMM", "dddd, d MMM", "dddd, d MMMM" };
        private readonly List<string> langCodes = new List<string>();
        private bool restartRequired;

        public event EventHandler UpdateSettings;

        public Options()
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
                //Root.Margin = new Thickness(0,25,0,0);
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

            ShowFeelsLikeCheckBox.IsChecked = Globals.Settings.ShowFeelsLike;

            foreach (var p in Globals.WpManager.Providers)
            {
                WeatherProvidersBox.Items.Add(p.Name);
            }

            WeatherProvidersBox.SelectedIndex = Globals.WpManager.Providers.IndexOf(Globals.WpManager.CurrentProvider);
            WeatherIntervalSlider.Value = Globals.Settings.RefreshInterval;

            if (Globals.Settings.TempScale == TemperatureScale.Celsius)
                CelsiusRadioButton.IsChecked = true;

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

            LanguageComboBox.Text = CultureInfo.GetCultureInfo(Globals.Settings.Language).NativeName;

            switch (Globals.Settings.Style)
            {
                case Styles.Large:
                    LargeStyle.IsChecked = true;
                    break;
                case Styles.Medium:
                    MediumStyle.IsChecked = true;
                    break;
                case Styles.Small:
                    SmallStyle.IsChecked = true;
                    break;
                case Styles.Metro:
                    MetroStyle.IsChecked = true;
                    break;
            }

            SizeSlider.Value = Math.Round(Globals.Settings.Scale * 100);
            SizeValueTextBlock.Text = SizeSlider.Value + "%";

            TransparencySlider.Value = Math.Round((1 - Globals.Settings.Opacity) * 100);
            TransparencyValueTextBlock.Text = TransparencySlider.Value + "%";

            UpdateFreqSlider.Value = Globals.Settings.UpdateInterval;
            UpdateFreqValueTextBlock.Text = UpdateFreqSlider.Value + " " + Properties.Resources.OptionsIntervalMinutes;

            UseAeroCheckBox.IsChecked = Globals.Settings.UseAero;

            ShowTaskbarIconCheckBox.IsChecked = !Globals.Settings.UseTrayIcon;
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

            UpdatesCheckBox.IsChecked = Globals.Settings.CheckForUpdates;
            ShowHWCheckBox.IsChecked = Globals.Settings.ShowHW;
            AutostartCheckBox.IsChecked = Globals.Settings.Autostart;

            switch (Globals.Settings.WindSpeedScale)
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

            //var updates = Home.Updates.Updater.GetInstalledUpdatesInfoList();
            //foreach (var updateInfo in updates)
            //{
            //    UpdatesList.Items.Add(updateInfo);
            //}

            SilentUpdateCheckBox.IsChecked = Globals.Settings.SilentUpdate;
            if (Globals.SoundPlayer == null)
                PlaySoundsAnimCheckBox.Visibility = System.Windows.Visibility.Collapsed;
            PlaySoundsAnimCheckBox.IsChecked = Globals.Settings.EnableSounds;

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
                Globals.Settings.LocationCode = currentLocation.Code;
            }
            Globals.Settings.ShowFeelsLike = (bool)ShowFeelsLikeCheckBox.IsChecked;
            Globals.Settings.RefreshInterval = WeatherIntervalSlider.Value;
            Globals.Settings.TempScale = (bool)CelsiusRadioButton.IsChecked ? TemperatureScale.Celsius : TemperatureScale.Fahrenheit;
            Globals.Settings.Scale = SizeSlider.Value / 100;
            Globals.Settings.Opacity = 1 - (TransparencySlider.Value / 100);
            Globals.Settings.UseAero = (bool)UseAeroCheckBox.IsChecked;
            Globals.Settings.UseTrayIcon = !(bool)ShowTaskbarIconCheckBox.IsChecked;
            Globals.Settings.Provider = WeatherProvidersBox.Text;
            Globals.Settings.CheckForUpdates = (bool)UpdatesCheckBox.IsChecked;
            Globals.Settings.ShowHW = (bool)ShowHWCheckBox.IsChecked;
            Globals.Settings.SilentUpdate = (bool)SilentUpdateCheckBox.IsChecked;
            Globals.Settings.EnableSounds = (bool)PlaySoundsAnimCheckBox.IsChecked;

            if (Globals.Settings.UpdateInterval != UpdateFreqSlider.Value)
            {
                //App.UpdateTimer.Interval = TimeSpan.FromMinutes(UpdateFreqSlider.Value);
                //App.UpdateTimer.Stop();
                //App.UpdateTimer.Start();
            }

            if (!Globals.Settings.CheckForUpdates)
            {
                //App.UpdateTimer.Stop();
            }

            Globals.Settings.UpdateInterval = (int)UpdateFreqSlider.Value;

            //if (Globals.Settings.UseTrayIcon)
            //    ((App)Application.Current).AddTrayIcon();
            //else
            //    ((App)Application.Current).RemoveTrayIcon();

            var lastStyle = Globals.Settings.Style;
            if ((bool)LargeStyle.IsChecked)
                Globals.Settings.Style = Styles.Large;

            if ((bool)MediumStyle.IsChecked)
                Globals.Settings.Style = Styles.Medium;

            if ((bool)SmallStyle.IsChecked)
                Globals.Settings.Style = Styles.Small;

            if ((bool)MetroStyle.IsChecked)
                Globals.Settings.Style = Styles.Metro;

            foreach (WeatherProvider p in Globals.WpManager.Providers)
            {
                if (p.Name == Globals.Settings.Provider)
                {
                    if (!p.IsLoaded)
                        p.Load();
                    Globals.WpManager.CurrentProvider = p;
                }
            }

            restartRequired = lastStyle != Globals.Settings.Style;

            var lastLang = Globals.Settings.Language;
            if (LanguageComboBox.SelectedIndex >= 0)
                Globals.Settings.Language = langCodes[LanguageComboBox.SelectedIndex];

            if (MphRadioButton.IsChecked == true)
                Globals.Settings.WindSpeedScale = WindSpeedScale.Mph;
            else if (KmhRadioButton.IsChecked == true)
                Globals.Settings.WindSpeedScale = WindSpeedScale.Kmh;
            else
                Globals.Settings.WindSpeedScale = WindSpeedScale.Ms;


            if (!restartRequired)
                restartRequired = lastLang != Globals.Settings.Language;

            if (Globals.Settings.Autostart != (bool)AutostartCheckBox.IsChecked)
            {
                Globals.Settings.Autostart = (bool)AutostartCheckBox.IsChecked;
                if (Globals.Settings.Autostart)
                {
                    try
                    {
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", RegistryKeyPermissionCheck.ReadWriteSubTree).OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Run", true))
                        {
                            key.SetValue("Weather Widget (HTC Home)", "\"" + Assembly.GetExecutingAssembly().Location + "\"", RegistryValueKind.String);
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
                            key.DeleteValue("Weather Widget (HTC Home)", false);
                            key.Close();
                        }
                    }
                    catch { }
                }
            }

            Globals.Settings.Save(Globals.ConfigFile);

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
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(SearchBox.Text) && SearchBox.Text.Length > 2)
            {

                SearchPopup.IsOpen = true;
                searchDelayTimer.Stop();
                searchDelayTimer.Start();
                SearchResultBox.Items.Clear();
                SearchProgress.Visibility = System.Windows.Visibility.Visible;
                SearchButton.Source = new BitmapImage(new Uri("/Resources/Weather/SearchCancelIcon.png", UriKind.Relative));
            }
        }

        private void GetLocations(string query)
        {
            locations = Globals.WpManager.CurrentProvider.GetLocations(query, CultureInfo.GetCultureInfo(Globals.Settings.Language), Globals.Settings.TempScale);
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

        void ItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var index = SearchResultBox.Items.IndexOf(sender);
            currentLocation = locations[index];
            SearchBox.Text = currentLocation.City;
            searchDelayTimer.Stop();
            SearchPopup.IsOpen = false;
            ApplyButton.IsEnabled = true;
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
                SearchButton.Source = new BitmapImage(new Uri("/Resources/Weather/SearchCancelIcon.png", UriKind.Relative));
            }
            else
            {
                SearchPopup.IsOpen = false;
                SearchResultBox.Items.Clear();
                SearchProgress.Visibility = System.Windows.Visibility.Collapsed;
                searchDelayTimer.Stop();
                SearchButton.Source = new BitmapImage(new Uri("/Resources/Weather/SearchIcon.png", UriKind.Relative));
            }
        }

        private void ItemSelected(object sender, RoutedEventArgs e)
        {
            var index = SearchResultBox.Items.IndexOf(sender);
            currentLocation = locations[index];
            SearchBox.Text = currentLocation.City;
            searchDelayTimer.Stop();
            SearchPopup.IsOpen = false;
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
            SearchButton.Source = new BitmapImage(new Uri("/Resources/Weather/SearchIcon.png", UriKind.Relative));
            e.Handled = true;
        }

        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyButton.IsEnabled = true;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Globals.Settings.OptionsWidth = Width;
            Globals.Settings.OptionsHeight = Height;

            if (restartRequired)
            {
                Process.Start(Application.ResourceAssembly.Location, "/c \"" + Globals.ConfigFile + "\"");
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
                //var packageManager = new PackageManager();
                //packageManager.BeginUnpack(d.FileName, E.Root);
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

            var args = Globals.Settings.Language + " /wcheck";
            Process.Start(E.Root + "\\Update.exe", args);
        }
    }
}
