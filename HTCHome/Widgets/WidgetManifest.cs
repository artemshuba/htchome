namespace HTCHome.Widgets
{
    sealed record WidgetManifest
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public required string AssemblyName { get; init; }
    }
}