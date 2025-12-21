using Home.Base.Mvvm;
using System;
using System.Windows.Threading;

namespace Clock.ViewModel
{
    public class WidgetViewModel : BindableBase
    {
        private DispatcherTimer _clockTimer;

        private int _hours;
        private int _minutes;
        private int _seconds;
        private double _hourAngle;
        private double _minuteAngle;
        private double _secondAngle;
        private string _dateString;
        private string _amPm;

        public int Hours { get { return _hours; } set { Set(ref _hours, value); } }
        public int Minutes { get { return _minutes; } set { Set(ref _minutes, value); } }
        public int Seconds { get { return _seconds; } set { Set(ref _seconds, value); } }
        
        // Angles for Analog Clock
        public double HourAngle { get { return _hourAngle; } set { Set(ref _hourAngle, value); } }
        public double MinuteAngle { get { return _minuteAngle; } set { Set(ref _minuteAngle, value); } }
        public double SecondAngle { get { return _secondAngle; } set { Set(ref _secondAngle, value); } }

        public string DateString { get { return _dateString; } set { Set(ref _dateString, value); } }
        public string AmPm { get { return _amPm; } set { Set(ref _amPm, value); } }

        public WidgetViewModel()
        {
            UpdateClock();
            _clockTimer = new DispatcherTimer(TimeSpan.FromSeconds(0.1), DispatcherPriority.Normal, (s, e) => UpdateClock(), Dispatcher.CurrentDispatcher);
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            Hours = now.Hour;
            Minutes = now.Minute;
            Seconds = now.Second;

            DateString = now.ToString("ddd, MMM d").ToUpper(); // e.g. MON, JAN 1
            AmPm = now.ToString("tt");

            // Analog Logic
            // Hour hand moves with minutes: 30 degrees per hour + 0.5 degrees per minute
            HourAngle = (now.Hour % 12) * 30 + now.Minute * 0.5;
            // Minute hand moves with seconds: 6 degrees per minute + 0.1 degrees per second
            MinuteAngle = now.Minute * 6 + now.Second * 0.1;
            // Second hand: 6 degrees per second
            SecondAngle = now.Second * 6;
        }
    }
}
