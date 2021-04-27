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
using Home.Packaging;
using Microsoft.Win32;
using Weather.Base;
using Path = System.IO.Path;

namespace Clock.Windows
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class DualClockOptions : Window
    {
        private readonly List<string> langCodes = new List<string>();
        private bool restartRequired;

        public event EventHandler UpdateSettings;

        public DualClockOptions()
        {
            InitializeComponent();
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

            foreach (var timeZone in TimeZoneInfo.GetSystemTimeZones())
            {
                FirstTimeZoneComboBox.Items.Add(new ComboBoxItem() { Content = timeZone.DisplayName });
                SecondTimeZoneComboBox.Items.Add(new ComboBoxItem() { Content = timeZone.DisplayName });
                if (timeZone.Id == App.Settings.FirstTimeZoneId)
                    FirstTimeZoneComboBox.Text = timeZone.DisplayName;
                if (timeZone.Id == App.Settings.SecondTimeZoneId)
                    SecondTimeZoneComboBox.Text = timeZone.DisplayName;
            }

            if (string.IsNullOrEmpty(FirstTimeZoneComboBox.Text))
                FirstTimeZoneComboBox.Text = TimeZoneInfo.Local.DisplayName;
            if (string.IsNullOrEmpty(SecondTimeZoneComboBox.Text))
                SecondTimeZoneComboBox.Text = TimeZoneInfo.Local.DisplayName;

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

            FirstClockNameBox.Text = App.Settings.FirstClockName;
            SecondClockNameBox.Text = App.Settings.SecondClockName;

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
            var updateTime = false;
            App.Settings.FirstClockName = FirstClockNameBox.Text;
            App.Settings.SecondClockName = SecondClockNameBox.Text;
            App.Settings.Scale = SizeSlider.Value / 100;
            App.Settings.Opacity = 1 - (TransparencySlider.Value / 100);
            App.Settings.UseAero = (bool)UseAeroCheckBox.IsChecked;
            App.Settings.UseTrayIcon = !(bool)ShowTaskbarIconCheckBox.IsChecked;
            App.Settings.CheckForUpdates = (bool)UpdatesCheckBox.IsChecked;
            App.Settings.SilentUpdate = (bool)SilentUpdateCheckBox.IsChecked;

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
                App.Settings.Style = Styles.ModernFlipClock;
            if ((bool)WideFlipClockStyle.IsChecked)
                App.Settings.Style = Styles.WideFlipClock;
            if ((bool)NoWeatherFlipClockStyle.IsChecked)
                App.Settings.Style = Styles.FlipClockNoWeather;

            restartRequired = lastStyle != App.Settings.Style;

            var lastLang = App.Settings.Language;
            if (LanguageComboBox.SelectedIndex >= 0)
                App.Settings.Language = langCodes[LanguageComboBox.SelectedIndex];
            if (!restartRequired)
                restartRequired = lastLang != App.Settings.Language;

            if (FirstTimeZoneComboBox.SelectedIndex >= 0)
            {
                var timeZone = TimeZoneInfo.GetSystemTimeZones()[FirstTimeZoneComboBox.SelectedIndex];
                if (timeZone.Id != App.Settings.FirstTimeZoneId)
                {
                    updateTime = true;
                    App.Settings.FirstTimeZoneId = timeZone.Id;
                    App.Settings.FirstClockName = timeZone.DisplayName;
                    FirstClockNameBox.Text = timeZone.DisplayName;
                }
            }

            if (SecondTimeZoneComboBox.SelectedIndex >= 0)
            {
                var timeZone = TimeZoneInfo.GetSystemTimeZones()[SecondTimeZoneComboBox.SelectedIndex];
                if (timeZone.Id != App.Settings.SecondTimeZoneId)
                {
                    updateTime = true;
                    App.Settings.SecondTimeZoneId = timeZone.Id;
                    App.Settings.SecondClockName = timeZone.DisplayName;
                    SecondClockNameBox.Text = timeZone.DisplayName;
                }
            }

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
                UpdateSettings(updateTime, EventArgs.Empty);
            }
        }

        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyButton.IsEnabled = true;
        }

        private void ClockNameBoxTextChanged(object sender, TextChangedEventArgs e)
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

        private void DeleteExtButtonClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.OptionsExtrasDeleteMessage, "HTC Home", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
            {
                ExtrasManager.DeleteExtra((ExtrasInfo)ExtrasList.SelectedItem);
                ExtrasList.Items.Remove(ExtrasList.SelectedItem);
            }
        }

        private void ExtrasListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && ((ExtrasInfo)e.AddedItems[0]).Removable)
                DeleteExtButton.IsEnabled = true;
            else
                DeleteExtButton.IsEnabled = false;
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
