using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Textie.Core.Configuration;
using Textie.Core.Scheduling;

namespace Textie.Core.Abstractions;

public interface IConfigurationStore
{
    Task<SpamConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken);
    Task SaveConfigurationAsync(SpamConfiguration configuration, CancellationToken cancellationToken);
    Task<IReadOnlyList<SpamProfile>> LoadProfilesAsync(CancellationToken cancellationToken);
    Task SaveProfilesAsync(IEnumerable<SpamProfile> profiles, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScheduledRun>> LoadSchedulesAsync(CancellationToken cancellationToken);
    Task SaveSchedulesAsync(IEnumerable<ScheduledRun> schedules, CancellationToken cancellationToken);
}
