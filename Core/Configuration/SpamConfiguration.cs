using System;
using System.ComponentModel.DataAnnotations;

namespace Textie.Core.Configuration;
    public class SpamConfiguration
    {
        [Required]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters")]
        public string Message { get; set; } = string.Empty;

        [Range(1, 10000, ErrorMessage = "Count must be between 1 and 10,000")]
        public int Count { get; set; } = 1;

        [Range(0, 120000, ErrorMessage = "Delay must be between 0 and 120,000 milliseconds")]
        public int DelayMilliseconds { get; set; } = 100;

        [Range(0, 100, ErrorMessage = "Delay jitter must be between 0% and 100%")]
        public int DelayJitterPercent { get; set; } = 0;

        [Range(0, 500, ErrorMessage = "Per-character delay must be between 0 and 500 milliseconds")]
        public int PerCharacterDelayMilliseconds { get; set; } = 15;

        public SpamStrategy Strategy { get; set; } = SpamStrategy.SendTextAndEnter;

        public bool SendSubmitKey { get; set; } = true;

        public bool LockTargetWindow { get; set; }

        public string? TargetWindowTitle { get; set; }

        public bool EnableTemplating { get; set; } = true;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Message))
                return false;

            if (Count <= 0 || Count > 10000)
                return false;

            if (DelayMilliseconds < 0 || DelayMilliseconds > 120000)
                return false;

            if (DelayJitterPercent < 0 || DelayJitterPercent > 100)
                return false;

            if (PerCharacterDelayMilliseconds < 0 || PerCharacterDelayMilliseconds > 500)
                return false;

            if (LockTargetWindow && string.IsNullOrWhiteSpace(TargetWindowTitle))
                return false;

            return true;
        }

        public SpamConfiguration Clone() => new()
        {
            Message = Message,
            Count = Count,
            DelayMilliseconds = DelayMilliseconds,
            DelayJitterPercent = DelayJitterPercent,
            PerCharacterDelayMilliseconds = PerCharacterDelayMilliseconds,
            Strategy = Strategy,
            SendSubmitKey = SendSubmitKey,
            LockTargetWindow = LockTargetWindow,
            TargetWindowTitle = TargetWindowTitle,
            EnableTemplating = EnableTemplating
        };

        public override string ToString() =>
            $"Message: \"{Message}\", Count: {Count}, Delay: {DelayMilliseconds}ms, Strategy: {Strategy}";
    }
