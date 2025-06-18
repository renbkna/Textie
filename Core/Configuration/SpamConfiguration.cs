using System;
using System.ComponentModel.DataAnnotations;

namespace Textie.Core.Configuration
{
    public class SpamConfiguration
    {
        [Required]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 1000 characters")]
        public string Message { get; set; } = string.Empty;

        [Range(1, 10000, ErrorMessage = "Count must be between 1 and 10,000")]
        public int Count { get; set; } = 1;

        [Range(0, 60000, ErrorMessage = "Delay must be between 0 and 60,000 milliseconds")]
        public int DelayMilliseconds { get; set; } = 100;

        public bool IsValid() => !string.IsNullOrWhiteSpace(Message) && Count > 0 && DelayMilliseconds >= 0;

        public SpamConfiguration Clone() => new()
        {
            Message = Message,
            Count = Count,
            DelayMilliseconds = DelayMilliseconds
        };

        public override string ToString() =>
            $"Message: \"{Message}\", Count: {Count}, Delay: {DelayMilliseconds}ms";
    }
}
