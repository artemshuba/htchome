using Clock.ViewModel;
using System.Windows.Controls;

namespace Clock
{
    /// <summary>
    /// Interaction logic for WidgetView.xaml
    /// </summary>
    public partial class WidgetView : UserControl
    {
        public WidgetViewModel ViewModel => (WidgetViewModel)DataContext;

        public WidgetView()
        {
            InitializeComponent();
        }
    }
}