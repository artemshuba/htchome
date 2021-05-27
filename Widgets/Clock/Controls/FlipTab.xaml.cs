using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Clock.Domain;

namespace Clock.Controls
{
    /// <summary>
    /// Interaction logic for FlipTab.xaml
    /// </summary>
    public partial class FlipTab : UserControl
    {
        private const string Path = "pack://application:,,,/Clock;component/Resources/FlipClock/Digits/{0}.png";

        private bool _isFlipping = false;
        private bool _isInitialValueChange = true;

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int), typeof(FlipTab), new PropertyMetadata(default(int), OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FlipTab)d;
            control.Flip((int)e.NewValue, !control._isInitialValueChange);
            control._isInitialValueChange = false;
        }

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public event EventHandler FlipCompleted;

        private double speed = 1.0f;
        public double Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                var s = (Storyboard)this.Resources["FlipAnim"];
                ((DoubleAnimation)s.Children[0]).Duration = TimeSpan.FromSeconds(0.7f * value);
                ((DoubleAnimation)s.Children[1]).Duration = TimeSpan.FromSeconds(0.35f * value);
            }
        }

        private double delay = 0;
        public double Delay
        {
            get { return delay; }
            set
            {
                var s = (Storyboard)this.Resources["FlipAnim"];
                s.BeginTime = TimeSpan.FromSeconds(value);
                delay = value;
            }
        }

        private TimeMode timeMode = TimeMode.None;
        public TimeMode TimeMode
        {
            get { return timeMode; }
            set
            {
                //switch (value)
                //{
                //    case TimeMode.None:
                //        AmPm.Visibility = System.Windows.Visibility.Hidden;
                //        AmPmBack.Opacity = 0;
                //        break;
                //    case TimeMode.Am:
                //        AmPm.Visibility = System.Windows.Visibility.Visible;
                //        AmPmBack.Opacity = 1;
                //        AmPmBack.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Clock;component/Resources/FlipClock/am.png"));
                //        break;
                //    case TimeMode.Pm:
                //        AmPm.Visibility = System.Windows.Visibility.Visible;
                //        AmPmBack.Opacity = 1;
                //        AmPmBack.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Clock;component/Resources/FlipClock/pm.png"));
                //        break;
                //}
                timeMode = value;
            }
        }

        public FlipTab()
        {
            InitializeComponent();

            //var bi = new BitmapImage();
            //bi.BeginInit();
            //bi.UriSource = new Uri(string.Format("pack://application:,,,/Clock;component/Resources/FlipClock/am.png"));
            //bi.EndInit();
            //AmPmBack.ImageSource = bi;
        }

        public void Flip(int newValue, bool animated = true)
        {
            if (_isFlipping)
                return;

            _isFlipping = true;

            var firstDigit = GetFirstDigit(newValue);
            var lastDigit = GetLastDigit(newValue);

            if (animated)
            {
                BgLeftDigitTop.Source = new BitmapImage(new Uri(string.Format(Path, firstDigit), UriKind.Absolute));
                BgRightDigitTop.Source = new BitmapImage(new Uri(string.Format(Path, lastDigit), UriKind.Absolute));
            }
            else
            {
                BgLeftDigitBottom.Source = new BitmapImage(new Uri(string.Format(Path, firstDigit), UriKind.Absolute));
                BgRightDigitBottom.Source = new BitmapImage(new Uri(string.Format(Path, lastDigit), UriKind.Absolute));
            }

            //if (timeMode != -1)
            //{
            //    if (timeMode == 0)
            //        AmPmBack.ImageSource = new BitmapImage(new Uri("am.png"));
            //    else
            //        AmPmBack.ImageSource = new BitmapImage(new Uri("pm.png"));
            //}

            if (TimeMode != TimeMode.None && firstDigit == 0)
            {
                //BgLeftDigitGrid.Visibility = System.Windows.Visibility.Collapsed;
                //LeftDigitBottomBrush.Opacity = 0;
                //LeftDigitTopBrush.Opacity = 0;
                //RightDigitTopTranslate.X = 0.285;
                //RightDigitBottomTranslate.X = 0.285;
            }

            else
            {
                //BgLeftDigitGrid.Visibility = System.Windows.Visibility.Visible;
                //LeftDigitBottomBrush.Opacity = 1;
                //LeftDigitTopBrush.Opacity = 1;

                //RightDigitTopTranslate.X = 0.495;
                //RightDigitBottomTranslate.X = 0.495;
            }
            //if (TimeMode == TimeMode.Am)
            //    AmPmBack.ImageSource = new BitmapImage(new Uri("am.png"));
            //else
            //    AmPmBack.ImageSource = new BitmapImage(new Uri("pm.png"));

            if (animated) 
            {
                var s = (Storyboard)this.Resources["FlipAnim"];
                s.Begin();
            } else
            {
                _isFlipping = false;
            }

            //Value = d;
        }

        private static int GetFirstDigit(int n)
        {
            int result = n;
            if (result > 9)
            {
                return (int)(result / 10);
            }
            else
                return 0;
        }

        private static int GetLastDigit(int n)
        {
            int result = n;
            if (result > 9)
            {
                return GetRemainder(result, 10);
            }
            else
                return result;
        }

        private static int GetRemainder(int a, int b)
        {
            var result = (int)(a / b);
            return a - result * b;
        }

        private void FlipAnimCompleted(object sender, EventArgs e)
        {
            BgLeftDigitBottom.Source = BgLeftDigitTop.Source;
            BgRightDigitBottom.Source = BgRightDigitTop.Source;

            AmPm.Source = AmPmBack.ImageSource;

            if (FlipCompleted != null)
                FlipCompleted(this, EventArgs.Empty);

            _isFlipping = false;
        }
    }
}
