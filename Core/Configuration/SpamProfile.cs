using System;

namespace Textie.Core.Configuration
{
    public class SpamProfile
    {
        public string Name { get; set; } = string.Empty;
        public SpamConfiguration Configuration { get; set; } = new();
        public DateTimeOffset LastUsed { get; set; } = DateTimeOffset.UtcNow;
        public string? Notes { get; set; }
    }
}
