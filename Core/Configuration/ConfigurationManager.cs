using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Textie.Core.Abstractions;

namespace Textie.Core.Configuration;
    public class ConfigurationManager : IDisposable
    {
        private readonly IConfigurationStore _store;
        private readonly ILogger<ConfigurationManager> _logger;
        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private bool _disposed;

        private SpamConfiguration _currentConfiguration = new();
        private List<SpamProfile> _profiles = [];

        public ConfigurationManager(IConfigurationStore store, ILogger<ConfigurationManager> logger)
        {
            _store = store;
            _logger = logger;
        }

        public SpamConfiguration CurrentConfiguration => _currentConfiguration.Clone();

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                _currentConfiguration = await _store.LoadConfigurationAsync(cancellationToken);
                var profiles = await _store.LoadProfilesAsync(cancellationToken);
                _profiles = profiles.ToList();
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task UpdateConfigurationAsync(SpamConfiguration configuration, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(configuration);
            if (!configuration.IsValid()) throw new ArgumentException("Configuration is invalid", nameof(configuration));

            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                _currentConfiguration = configuration.Clone();
                await _store.SaveConfigurationAsync(_currentConfiguration, cancellationToken);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task<IReadOnlyList<SpamProfile>> GetProfilesAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                return _profiles.Select(p => new SpamProfile
                {
                    Name = p.Name,
                    Notes = p.Notes,
                    LastUsed = p.LastUsed,
                    Configuration = p.Configuration.Clone()
                }).ToList();
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task SaveProfileAsync(SpamProfile profile, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(profile);
            if (!profile.Configuration.IsValid()) throw new ArgumentException("Profile configuration invalid", nameof(profile));

            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                var existing = _profiles.FirstOrDefault(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Configuration = profile.Configuration.Clone();
                    existing.LastUsed = DateTimeOffset.UtcNow;
                    existing.Notes = profile.Notes;
                }
                else
                {
                    _profiles.Add(new SpamProfile
                    {
                        Name = profile.Name,
                        Notes = profile.Notes,
                        LastUsed = DateTimeOffset.UtcNow,
                        Configuration = profile.Configuration.Clone()
                    });
                }

                await _store.SaveProfilesAsync(_profiles, cancellationToken);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task DeleteProfileAsync(string profileName, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (string.IsNullOrWhiteSpace(profileName)) return;

            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                _profiles = _profiles
                    .Where(p => !string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                await _store.SaveProfilesAsync(_profiles, cancellationToken);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task ResetToDefaultsAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _syncLock.WaitAsync(cancellationToken);
            try
            {
                _currentConfiguration = new SpamConfiguration();
                await _store.SaveConfigurationAsync(_currentConfiguration, cancellationToken);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _syncLock.Dispose();
            _disposed = true;
        }
    }
