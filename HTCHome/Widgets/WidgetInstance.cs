using Home.Base.Widgets;
using System.Runtime.Loader;
using System.Windows;

namespace HTCHome.Widgets
{
    sealed class WidgetInstance
    {
        public required string InstanceId { get; init; }

        public required string WidgetId { get; init; }

        public required IWidget Widget { get; init; }

        public required WidgetWindow Window { get; init; }

        public required AssemblyLoadContext AssemblyLoadContext { get; init; }
    }
}
