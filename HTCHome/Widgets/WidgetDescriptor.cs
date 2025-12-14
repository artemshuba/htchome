using System;
using System.Collections.Generic;
using System.Text;

namespace HTCHome.Widgets
{
    sealed record WidgetDescriptor
    {
        public required WidgetManifest Manifest { get; init; }

        public required string DirectoryPath { get; init; }
    }
}
