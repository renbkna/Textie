using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Textie.Core.Abstractions;
using Textie.Core.Configuration;
using Textie.Core.Spammer;
using Textie.Core.UI;

namespace Textie.Core
{
    public class TextieApplication
    {
        private readonly ConfigurationManager _configurationManager;
        private readonly IUserInterface _ui;
        private readonly IHotkeyService _hotkeys;
        private readonly TextSpammerEngine _spammerEngine;
        private readonly ILogger<TextieApplication> _logger;

        public TextieApplication(
            ConfigurationManager configurationManager,
            IUserInterface ui,
            IHotkeyService hotkeys,
            TextSpammerEngine spammerEngine,
            ILogger<TextieApplication> logger)
        {
            _configurationManager = configurationManager;
            _ui = ui;
            _hotkeys = hotkeys;
            _spammerEngine = spammerEngine;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await _configurationManager.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await _hotkeys.InitializeAsync(cancellationToken).ConfigureAwait(false);

            _ui.Initialize();

            var running = true;
            while (running && !cancellationToken.IsCancellationRequested)
            {
                var currentConfig = _configurationManager.CurrentConfiguration;
                var profiles = await _configurationManager.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
                var wizardResult = await _ui.RunConfigurationWizardAsync(currentConfig, profiles, cancellationToken).ConfigureAwait(false);

                if (wizardResult.IsCancelled)
                {
                    var continueApp = await _ui.PromptNextActionAsync(cancellationToken).ConfigureAwait(false);
                    if (continueApp == NextAction.Exit)
                    {
                        break;
                    }

                    continue;
                }

                try
                {
                    await _configurationManager.UpdateConfigurationAsync(wizardResult.Configuration, cancellationToken).ConfigureAwait(false);
                    if (wizardResult.SaveProfile && !string.IsNullOrWhiteSpace(wizardResult.ProfileName))
                    {
                        await _configurationManager.SaveProfileAsync(new SpamProfile
                        {
                            Name = wizardResult.ProfileName!,
                            Configuration = wizardResult.Configuration,
                            Notes = wizardResult.SelectedProfile?.Notes
                        }, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _ui.ShowError("Failed to persist configuration", ex);
                    _logger.LogError(ex, "Failed to persist configuration.");
                    continue;
                }

                currentConfig = wizardResult.Configuration;
                profiles = await _configurationManager.GetProfilesAsync(cancellationToken).ConfigureAwait(false);

                await _ui.ShowWaitingDashboardAsync(currentConfig, profiles, cancellationToken).ConfigureAwait(false);

                HotkeyAction action;
                do
                {
                    action = await _hotkeys.WaitForNextAsync(cancellationToken).ConfigureAwait(false);
                    if (action == HotkeyAction.Exit)
                    {
                        return;
                    }
                } while (action != HotkeyAction.Start);

                var summary = await _ui.RunAutomationAsync(currentConfig, _spammerEngine, token => ExecuteAutomationAsync(currentConfig, token), cancellationToken).ConfigureAwait(false);
                _hotkeys.SignalCompletion();

                if (summary != null)
                {
                    _ui.ShowRunSummary(summary);
                }

                var next = await _ui.PromptNextActionAsync(cancellationToken).ConfigureAwait(false);
                switch (next)
                {
                    case NextAction.RunAgain:
                        continue;
                    case NextAction.ChangeSettings:
                        continue;
                    case NextAction.Exit:
                        running = false;
                        break;
                }
            }

            _ui.Shutdown();
        }

        private async Task<SpamRunSummary?> ExecuteAutomationAsync(SpamConfiguration configuration, CancellationToken cancellationToken)
        {
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var focusLost = false;
            CancellationTokenSource? focusMonitorCts = null;
            Task? focusMonitorTask = null;

            if (configuration.LockTargetWindow && !string.IsNullOrWhiteSpace(configuration.TargetWindowTitle))
            {
                var expected = configuration.TargetWindowTitle!;
                var initialTitle = Input.WindowUtilities.GetForegroundWindowTitle();
                if (string.IsNullOrWhiteSpace(initialTitle) || !initialTitle.Contains(expected, StringComparison.OrdinalIgnoreCase))
                {
                    _ui.ShowError($"Focus the target window containing '{expected}' before starting.");
                    return new SpamRunSummary { Cancelled = true, FocusLost = true };
                }

                focusMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellation.Token);
                focusMonitorTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!focusMonitorCts.IsCancellationRequested)
                        {
                            await Task.Delay(250, focusMonitorCts.Token).ConfigureAwait(false);
                            var currentTitle = Input.WindowUtilities.GetForegroundWindowTitle();
                            if (string.IsNullOrWhiteSpace(currentTitle) || !currentTitle.Contains(expected, StringComparison.OrdinalIgnoreCase))
                            {
                                focusLost = true;
                                _spammerEngine.StopSpamming();
                                focusMonitorCts.Cancel();
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // expected on cancellation
                    }
                }, focusMonitorCts.Token);
            }

            var spamTask = _spammerEngine.StartSpammingAsync(configuration, linkedCancellation.Token);
            var waitTask = _hotkeys.WaitForNextAsync(linkedCancellation.Token);

            while (true)
            {
                var completed = await Task.WhenAny(spamTask, waitTask).ConfigureAwait(false);
                if (completed == spamTask)
                {
                    var summary = await spamTask.ConfigureAwait(false);
                    if (summary != null)
                    {
                        summary.FocusLost = focusLost;
                    }
                    await CleanupFocusMonitorAsync(focusMonitorCts, focusMonitorTask).ConfigureAwait(false);
                    return summary;
                }

                var action = await waitTask.ConfigureAwait(false);
                switch (action)
                {
                    case HotkeyAction.Stop:
                        _spammerEngine.StopSpamming();
                        linkedCancellation.Cancel();
                        {
                            var summary = await spamTask.ConfigureAwait(false);
                            if (summary != null)
                            {
                                summary.FocusLost = focusLost;
                            }
                            await CleanupFocusMonitorAsync(focusMonitorCts, focusMonitorTask).ConfigureAwait(false);
                            return summary;
                        }
                    case HotkeyAction.Exit:
                        _spammerEngine.StopSpamming();
                        linkedCancellation.Cancel();
                        {
                            var summary = await spamTask.ConfigureAwait(false);
                            if (summary != null)
                            {
                                summary.FocusLost = focusLost;
                            }
                            await CleanupFocusMonitorAsync(focusMonitorCts, focusMonitorTask).ConfigureAwait(false);
                            return summary;
                        }
                    case HotkeyAction.Start:
                    case HotkeyAction.None:
                        waitTask = _hotkeys.WaitForNextAsync(linkedCancellation.Token);
                        break;
                }
            }
        }

        private static async Task CleanupFocusMonitorAsync(CancellationTokenSource? cts, Task? monitorTask)
        {
            if (cts != null)
            {
                cts.Cancel();
            }

            if (monitorTask != null)
            {
                try
                {
                    await monitorTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // expected when cancelled
                }
            }
        }
    }
}
