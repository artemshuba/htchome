namespace HTCHome.Widgets
{
    sealed record WidgetManifest
    {
        public required string Id { get; init; }

        public required string DisplayName { get; init; }

        public required string AssemblyName { get; init; }

        /// <summary>
        /// If true widget will be loaded on initial startup.
        /// </summary>
        public required bool IsDefault { get; init; }
    }
}