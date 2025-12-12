using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Textie.Core.Abstractions;
using Textie.Core.Configuration;
using Textie.Core.Scheduling;
using Xunit;

namespace Textie.Tests;

public class ConfigurationManagerTests
{
    [Fact]
    public async Task InitializeAsync_LoadsConfiguration()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);

        await manager.InitializeAsync(CancellationToken.None);

        Assert.NotNull(manager.CurrentConfiguration);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_PersistsChanges()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var newConfig = new SpamConfiguration
        {
            Message = "Updated message",
            Count = 50,
            DelayMilliseconds = 100
        };

        await manager.UpdateConfigurationAsync(newConfig, CancellationToken.None);

        Assert.Equal("Updated message", manager.CurrentConfiguration.Message);
        Assert.Equal(50, manager.CurrentConfiguration.Count);
        Assert.True(store.ConfigurationSaved);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_RejectsInvalidConfig()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var invalidConfig = new SpamConfiguration { Message = "", Count = 0 };

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.UpdateConfigurationAsync(invalidConfig, CancellationToken.None));
    }

    [Fact]
    public async Task SaveProfileAsync_AddsNewProfile()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var profile = new SpamProfile
        {
            Name = "Test Profile",
            Configuration = new SpamConfiguration { Message = "Test", Count = 10, DelayMilliseconds = 50 }
        };

        await manager.SaveProfileAsync(profile, CancellationToken.None);
        var profiles = await manager.GetProfilesAsync(CancellationToken.None);

        Assert.Single(profiles);
        Assert.Equal("Test Profile", profiles[0].Name);
    }

    [Fact]
    public async Task SaveProfileAsync_UpdatesExistingProfile()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var profile1 = new SpamProfile
        {
            Name = "Test",
            Configuration = new SpamConfiguration { Message = "First", Count = 10, DelayMilliseconds = 50 }
        };
        var profile2 = new SpamProfile
        {
            Name = "Test", // Same name
            Configuration = new SpamConfiguration { Message = "Second", Count = 20, DelayMilliseconds = 100 }
        };

        await manager.SaveProfileAsync(profile1, CancellationToken.None);
        await manager.SaveProfileAsync(profile2, CancellationToken.None);
        var profiles = await manager.GetProfilesAsync(CancellationToken.None);

        Assert.Single(profiles); // Still one profile
        Assert.Equal("Second", profiles[0].Configuration.Message);
    }

    [Fact]
    public async Task DeleteProfileAsync_RemovesProfile()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var profile = new SpamProfile
        {
            Name = "ToDelete",
            Configuration = new SpamConfiguration { Message = "Test", Count = 10, DelayMilliseconds = 50 }
        };

        await manager.SaveProfileAsync(profile, CancellationToken.None);
        await manager.DeleteProfileAsync("ToDelete", CancellationToken.None);
        var profiles = await manager.GetProfilesAsync(CancellationToken.None);

        Assert.Empty(profiles);
    }

    [Fact]
    public async Task GetProfilesAsync_ReturnsClones()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);
        await manager.InitializeAsync(CancellationToken.None);

        var profile = new SpamProfile
        {
            Name = "Test",
            Configuration = new SpamConfiguration { Message = "Original", Count = 10, DelayMilliseconds = 50 }
        };

        await manager.SaveProfileAsync(profile, CancellationToken.None);
        var profiles1 = await manager.GetProfilesAsync(CancellationToken.None);
        profiles1[0].Configuration.Message = "Modified";
        var profiles2 = await manager.GetProfilesAsync(CancellationToken.None);

        // Modification shouldn't affect stored profile
        Assert.Equal("Original", profiles2[0].Configuration.Message);
    }

    [Fact]
    public void Dispose_AllowsMultipleCalls()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);

        manager.Dispose();
        manager.Dispose(); // Should not throw
    }

    [Fact]
    public async Task ThrowsAfterDispose()
    {
        var store = new InMemoryConfigurationStore();
        var manager = new ConfigurationManager(store, NullLogger<ConfigurationManager>.Instance);

        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.InitializeAsync(CancellationToken.None));
    }

    private sealed class InMemoryConfigurationStore : IConfigurationStore
    {
        public bool ConfigurationSaved { get; private set; }
        private SpamConfiguration _configuration = new() { Message = "Default", Count = 1, DelayMilliseconds = 100 };
        private readonly List<SpamProfile> _profiles = [];
        private readonly List<ScheduledRun> _schedules = [];

        public Task<SpamConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken)
            => Task.FromResult(_configuration.Clone());

        public Task SaveConfigurationAsync(SpamConfiguration configuration, CancellationToken cancellationToken)
        {
            _configuration = configuration.Clone();
            ConfigurationSaved = true;
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
