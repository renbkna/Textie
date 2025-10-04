using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Textie.Core.Configuration;
using Textie.Core.Spammer;

namespace Textie.Core.UI
{
    public enum NextAction
    {
        RunAgain,
        ChangeSettings,
        Exit
    }

    public record ConfigurationFlowResult(bool IsCancelled, SpamConfiguration Configuration, SpamProfile? SelectedProfile, bool SaveProfile, string? ProfileName);

    public interface IUserInterface
    {
        void Initialize();
        Task<ConfigurationFlowResult> RunConfigurationWizardAsync(SpamConfiguration current, IReadOnlyList<SpamProfile> profiles, CancellationToken cancellationToken);
        Task ShowWaitingDashboardAsync(SpamConfiguration configuration, IReadOnlyList<SpamProfile> profiles, CancellationToken cancellationToken);
        Task<SpamRunSummary?> RunAutomationAsync(SpamConfiguration configuration, TextSpammerEngine engine, Func<CancellationToken, Task<SpamRunSummary?>> runCallback, CancellationToken cancellationToken);
        void ShowRunSummary(SpamRunSummary summary);
        Task<NextAction> PromptNextActionAsync(CancellationToken cancellationToken);
        void ShowError(string message, System.Exception? exception = null);
        void Shutdown();
    }
}
