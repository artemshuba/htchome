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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Clock.Controls;
using Clock.Domain;
using Clock.WeatherAnimation;
using Clock.WeatherAnimationNew;
using Clock.WeatherFx;
using Home.Base;
using Weather.Base;
using Snowflake = Clock.WeatherAnimation.Snowflake;

namespace Clock.Windows
{
    /// <summary>
    /// Interaction logic for FlipClock.xaml
    /// </summary>
    public partial class FlipClockNoWeather : Window
    {
        private IntPtr handle;
        private DispatcherTimer clockTimer;
        private FlipClockOptions optionsWindow;
        private TimeSpan baseUtcOffset;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public FlipClockNoWeather()
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

            //DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);

            this.ShowInTaskbar = !App.Settings.UseTrayIcon;

            if (App.Settings.TopMost)
            {
                this.Topmost = true;
                TopMostItem.IsChecked = true;
            }

            if (App.Settings.Pin)
                PinItem.IsChecked = true;

        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Activate();

            clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += ClockTimerTick;
            //clockTimer starts when MinutesTab first flip completed


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

            var rgn = WinAPI.CreateRoundRectRgn((int)(App.Settings.Scale * dpiX), (int)(35 * App.Settings.Scale * dpiY),
                (int)(this.Width * App.Settings.Scale * dpiX), (int)((this.Height - 40) * App.Settings.Scale * dpiY),
                (int)(15 * App.Settings.Scale * dpiX), (int)(dpiY * App.Settings.Scale * 15));
            Dwm.MakeGlassRegion(ref handle, rgn);
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
                    //DateTextBlock.Text = DateTime.Now.ToString(App.Settings.DateFormat);
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
                    //DateTextBlock.Text = DateTime.Now.ToString(App.Settings.DateFormat);
                }
            }
            else
            {
                if (MinutesTab.Value != DateTime.Now.ToUniversalTime().Add(baseUtcOffset).Minute)
                {
                    MinutesTab.Flip(DateTime.Now.ToUniversalTime().Add(baseUtcOffset).Minute);
                    //DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);
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
                    //DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);
                }
            }
        }


        private void CloseItemClick(object sender, RoutedEventArgs e)
        {
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
           
            Scale.ScaleX = App.Settings.Scale;

            if (App.Settings.UseAero)
                UpdateAeroGlass();
            else
                Dwm.RemoveGlassRegion(ref handle);

            
            if (App.Settings.UseCustomTime)
            {
                baseUtcOffset = TimeZoneInfo.FindSystemTimeZoneById(App.Settings.FirstTimeZoneId).GetUtcOffset(DateTime.Now);
                //DateTextBlock.Text = DateTime.Now.ToUniversalTime().Add(baseUtcOffset).ToString(App.Settings.DateFormat);
            }
            else
            {
                baseUtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                //DateTextBlock.Text = DateTime.Now.ToString(App.Settings.DateFormat);
            }

            this.ShowInTaskbar = !App.Settings.UseTrayIcon;
            this.Opacity = App.Settings.Opacity;
        }

        private void WindowMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            App.Settings.Left = this.Left;
            App.Settings.Top = this.Top;
            App.Settings.Save(App.ConfigFile);
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

        private void PinItemClick(object sender, RoutedEventArgs e)
        {
            App.Settings.Pin = PinItem.IsChecked;
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
    }
}
