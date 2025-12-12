namespace Textie.Core.UI.Views;
    /// <summary>
    /// Strongly-typed delay option for configuration wizard.
    /// Replaces brittle string parsing with proper value binding.
    /// </summary>
    public sealed record DelayOption(string Label, int Milliseconds)
    {
        public static readonly DelayOption[] Presets =
        [
            new("Instant (0ms)", 0),
            new("Rapid (50ms)", 50),
            new("Standard (100ms)", 100),
            new("Measured (250ms)", 250),
            new("Moderate (500ms)", 500),
            new("Deliberate (1000ms)", 1000),
            new("Custom", -1) // Sentinel for custom input
        ];

        public bool IsCustom => Milliseconds < 0;

        public override string ToString() => Label;
    }
