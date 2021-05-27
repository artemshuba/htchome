using Home.Base.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clock.ViewModel
{
    public class WidgetViewModel : BindableBase
    {
        private DispatcherTimer _clockTimer;

        private int _hours = DateTime.Now.AddHours(-1).Hour;
        private int _minutes = DateTime.Now.AddMinutes(-2).Minute;

        public int Hours
        {
            get { return _hours; }
            set { Set(ref _hours, value); }
        }

        public int Minutes
        {
            get { return _minutes; }
            set { Set(ref _minutes, value); }
        }

        public WidgetViewModel()
        {
            _clockTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (s, e) =>
            {
                Hours = DateTime.Now.Hour;
                Minutes = DateTime.Now.Minute;
            }, Dispatcher.CurrentDispatcher);

            SetInitialTime();
        }

        private async void SetInitialTime()
        {
            var now = DateTime.Now;
            var time = now.AddMinutes(-1);

            await Task.Delay(300);

            Minutes = time.Minute;

            await Task.Delay(300);

            Hours = time.Hour;

            await Task.Delay(500);

            Minutes = now.Minute;
            _clockTimer.Start();
        }
    }
}
