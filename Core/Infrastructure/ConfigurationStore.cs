using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Textie.Core.Abstractions;
using Textie.Core.Configuration;
using Textie.Core.Scheduling;

namespace Textie.Core.Infrastructure
{
    public class ConfigurationStore : IConfigurationStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        private readonly string _settingsPath;
        private readonly string _profilesPath;
        private readonly string _schedulesPath;
        private readonly ILogger<ConfigurationStore> _logger;

        public ConfigurationStore(ILogger<ConfigurationStore> logger)
        {
            _logger = logger;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var root = Path.Combine(appData, "Textie");
            Directory.CreateDirectory(root);

            _settingsPath = Path.Combine(root, "settings.json");
            _profilesPath = Path.Combine(root, "profiles.json");
            _schedulesPath = Path.Combine(root, "schedules.json");
        }

        public async Task<SpamConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _logger.LogInformation("Settings file not found, loading defaults.");
                    return new SpamConfiguration();
                }

                await using var stream = File.OpenRead(_settingsPath);
                var config = await JsonSerializer.DeserializeAsync<SpamConfiguration>(stream, SerializerOptions, cancellationToken);
                if (config == null || !config.IsValid())
                {
                    _logger.LogWarning("Settings file invalid, using defaults.");
                    return new SpamConfiguration();
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration; using defaults.");
                return new SpamConfiguration();
            }
        }

        public async Task SaveConfigurationAsync(SpamConfiguration configuration, CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = File.Create(_settingsPath);
                await JsonSerializer.SerializeAsync(stream, configuration, SerializerOptions, cancellationToken);
                _logger.LogInformation("Configuration saved to {Path}.", _settingsPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration to {Path}.", _settingsPath);
            }
        }

        public async Task<IReadOnlyList<SpamProfile>> LoadProfilesAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(_profilesPath))
                {
                    _logger.LogInformation("Profile file not found, returning empty list.");
                    return Array.Empty<SpamProfile>();
                }

                await using var stream = File.OpenRead(_profilesPath);
                var profiles = await JsonSerializer.DeserializeAsync<List<SpamProfile>>(stream, SerializerOptions, cancellationToken);
                return profiles ?? new List<SpamProfile>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profiles; returning empty set.");
                return Array.Empty<SpamProfile>();
            }
        }

        public async Task SaveProfilesAsync(IEnumerable<SpamProfile> profiles, CancellationToken cancellationToken)
        {
            try
            {
                var list = profiles is List<SpamProfile> profileList ? profileList : new List<SpamProfile>(profiles);
                await using var stream = File.Create(_profilesPath);
                await JsonSerializer.SerializeAsync(stream, list, SerializerOptions, cancellationToken);
                _logger.LogInformation("Saved {Count} profiles to {Path}.", list.Count, _profilesPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save profiles to {Path}.", _profilesPath);
            }
        }

        public async Task<IReadOnlyList<ScheduledRun>> LoadSchedulesAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(_schedulesPath))
                {
                    return Array.Empty<ScheduledRun>();
                }

                await using var stream = File.OpenRead(_schedulesPath);
                var schedules = await JsonSerializer.DeserializeAsync<List<ScheduledRun>>(stream, SerializerOptions, cancellationToken);
                return schedules ?? new List<ScheduledRun>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load schedules; returning empty set.");
                return Array.Empty<ScheduledRun>();
            }
        }

        public async Task SaveSchedulesAsync(IEnumerable<ScheduledRun> schedules, CancellationToken cancellationToken)
        {
            try
            {
                var list = schedules is List<ScheduledRun> scheduleList ? scheduleList : new List<ScheduledRun>(schedules);
                await using var stream = File.Create(_schedulesPath);
                await JsonSerializer.SerializeAsync(stream, list, SerializerOptions, cancellationToken);
                _logger.LogInformation("Saved {Count} schedules to {Path}.", list.Count, _schedulesPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save schedules to {Path}.", _schedulesPath);
            }
        }
    }
}
