using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Textie.Core.Abstractions;
using Textie.Core.Configuration;
using Textie.Core.Scheduling;
using Xunit;

namespace Textie.Tests;

public class ScheduleManagerTests
{
    [Fact]
    public async Task InitializeAsync_LoadsSchedules()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ScheduleManager(store, NullLogger<ScheduleManager>.Instance);

        await manager.InitializeAsync(CancellationToken.None);
        var schedules = await manager.GetSchedulesAsync(CancellationToken.None);

        Assert.NotNull(schedules);
    }

    [Fact]
    public async Task AddOrUpdateAsync_AddsNewSchedule()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ScheduleManager(store, NullLogger<ScheduleManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var schedule = new ScheduledRun
        {
            Name = "TestSchedule",
            ProfileName = "TestProfile",
            CronExpression = "0 8 * * *"
        };

        await manager.AddOrUpdateAsync(schedule, CancellationToken.None);
        var schedules = await manager.GetSchedulesAsync(CancellationToken.None);

        Assert.Single(schedules);
        Assert.Equal("TestSchedule", schedules[0].Name);
    }

    [Fact]
    public async Task AddOrUpdateAsync_UpdatesExistingSchedule()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ScheduleManager(store, NullLogger<ScheduleManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var schedule1 = new ScheduledRun
        {
            Name = "Test",
            ProfileName = "Profile1",
            CronExpression = "0 8 * * *"
        };
        var schedule2 = new ScheduledRun
        {
            Name = "Test", // Same name
            ProfileName = "Profile2",
            CronExpression = "0 12 * * *"
        };

        await manager.AddOrUpdateAsync(schedule1, CancellationToken.None);
        await manager.AddOrUpdateAsync(schedule2, CancellationToken.None);
        var schedules = await manager.GetSchedulesAsync(CancellationToken.None);

        Assert.Single(schedules);
        Assert.Equal("Profile2", schedules[0].ProfileName);
        Assert.Equal("0 12 * * *", schedules[0].CronExpression);
    }

    [Fact]
    public async Task RemoveAsync_DeletesSchedule()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ScheduleManager(store, NullLogger<ScheduleManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var schedule = new ScheduledRun
        {
            Name = "ToDelete",
            ProfileName = "Test",
            CronExpression = "0 8 * * *"
        };

        await manager.AddOrUpdateAsync(schedule, CancellationToken.None);
        await manager.RemoveAsync("ToDelete", CancellationToken.None);
        var schedules = await manager.GetSchedulesAsync(CancellationToken.None);

        Assert.Empty(schedules);
    }

    [Fact]
    public async Task AddOrUpdateAsync_RejectsInvalidCron()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ScheduleManager(store, NullLogger<ScheduleManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var schedule = new ScheduledRun
        {
            Name = "BadCron",
            ProfileName = "Test",
            CronExpression = "not a valid cron"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.AddOrUpdateAsync(schedule, CancellationToken.None));
    }

    [Fact]
    public void ComputeNextRun_ReturnsValidDateTime()
    {
        var now = DateTimeOffset.Now;
        var nextRun = ScheduleManager.ComputeNextRun("0 8 * * *", now);

        Assert.NotNull(nextRun);
        Assert.True(nextRun > now, "Next run should be in the future");
    }

    [Fact]
    public void ComputeNextRun_RejectsInvalidCron()
    {
        Assert.Throws<ArgumentException>(
            () => ScheduleManager.ComputeNextRun("invalid cron", DateTimeOffset.Now));
    }

    [Fact]
    public void Dispose_AllowsMultipleCalls()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ScheduleManager(store, NullLogger<ScheduleManager>.Instance);

        manager.Dispose();
        manager.Dispose(); // Should not throw
    }

    [Fact]
    public async Task ThrowsAfterDispose()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ScheduleManager(store, NullLogger<ScheduleManager>.Instance);

        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.InitializeAsync(CancellationToken.None));
    }

    private sealed class InMemoryConfigurationStore : IConfigurationStore
    {
        private SpamConfiguration _configuration = new() { Message = "Default", Count = 1, DelayMilliseconds = 100 };
        private readonly List<SpamProfile> _profiles = [];
        private readonly List<ScheduledRun> _schedules = [];

        public Task<SpamConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken)
            => Task.FromResult(_configuration.Clone());

        public Task SaveConfigurationAsync(SpamConfiguration configuration, CancellationToken cancellationToken)
        {
            _configuration = configuration.Clone();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SpamProfile>> LoadProfilesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SpamProfile>>(_profiles.ToList());

        public Task SaveProfilesAsync(IEnumerable<SpamProfile> profiles, CancellationToken cancellationToken)
        {
            _profiles.Clear();
            _profiles.AddRange(profiles);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ScheduledRun>> LoadSchedulesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ScheduledRun>>(_schedules.ToList());

        public Task SaveSchedulesAsync(IEnumerable<ScheduledRun> schedules, CancellationToken cancellationToken)
        {
            _schedules.Clear();
            _schedules.AddRange(schedules);
            return Task.CompletedTask;
        }
    }
}
