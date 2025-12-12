using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCrontab;
using Textie.Core.Abstractions;

namespace Textie.Core.Scheduling;
    public class ScheduleManager : IDisposable
    {
        private readonly IConfigurationStore _store;
        private readonly ILogger<ScheduleManager> _logger;
        private readonly SemaphoreSlim _sync = new(1, 1);
        private List<ScheduledRun> _schedules = [];
        private bool _disposed;

        public ScheduleManager(IConfigurationStore store, ILogger<ScheduleManager> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _sync.WaitAsync(cancellationToken);
            try
            {
                var schedules = await _store.LoadSchedulesAsync(cancellationToken);
                _schedules = schedules.ToList();
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<IReadOnlyList<ScheduledRun>> GetSchedulesAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _sync.WaitAsync(cancellationToken);
            try
            {
                return _schedules.Select(Clone).ToList();
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task AddOrUpdateAsync(ScheduledRun schedule, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(schedule);
            ValidateCron(schedule.CronExpression);

            await _sync.WaitAsync(cancellationToken);
            try
            {
                var existing = _schedules.FirstOrDefault(s => string.Equals(s.Name, schedule.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.ProfileName = schedule.ProfileName;
                    existing.CronExpression = schedule.CronExpression;
                    existing.Enabled = schedule.Enabled;
                    existing.LastRun = schedule.LastRun;
                    existing.NextRun = schedule.NextRun;
                }
                else
                {
                    _schedules.Add(Clone(schedule));
                }

                await _store.SaveSchedulesAsync(_schedules, cancellationToken);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task RemoveAsync(string name, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrWhiteSpace(name)) return;

            await _sync.WaitAsync(cancellationToken);
            try
            {
                _schedules = _schedules
                    .Where(s => !string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                await _store.SaveSchedulesAsync(_schedules, cancellationToken);
            }
            finally
            {
                _sync.Release();
            }
        }

        public static DateTimeOffset? ComputeNextRun(string cron, DateTimeOffset from)
        {
            ValidateCron(cron);
            var schedule = CrontabSchedule.Parse(cron);
            var next = schedule.GetNextOccurrence(from.UtcDateTime);
            return new DateTimeOffset(next, TimeSpan.Zero).ToLocalTime();
        }

        private static void ValidateCron(string cron)
        {
            try
            {
                _ = CrontabSchedule.Parse(cron);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid cron expression", nameof(cron), ex);
            }
        }

        private static ScheduledRun Clone(ScheduledRun schedule) => new()
        {
            Name = schedule.Name,
            ProfileName = schedule.ProfileName,
            CronExpression = schedule.CronExpression,
            Enabled = schedule.Enabled,
            LastRun = schedule.LastRun,
            NextRun = schedule.NextRun
        };

        public void Dispose()
        {
            if (_disposed) return;
            _sync.Dispose();
            _disposed = true;
        }
    }
