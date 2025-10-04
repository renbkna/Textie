using System;

namespace Textie.Core.Scheduling
{
    public class ScheduledRun
    {
        public string Name { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string CronExpression { get; set; } = "0 0 * * *";
        public DateTimeOffset? LastRun { get; set; }
        public DateTimeOffset? NextRun { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
