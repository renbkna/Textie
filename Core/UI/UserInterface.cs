using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Textie.Core.Configuration;
using Textie.Core.Spammer;
using Textie.Core.UI.Views;

namespace Textie.Core.UI;
    public class UserInterface : IUserInterface
    {
        private readonly AppShell _shell;
        private readonly ConfigurationWizardView _wizardView;
        private readonly LiveDashboardView _dashboardView;
        private readonly RunSummaryView _summaryView;
        private readonly UiTheme _theme;

        public UserInterface(
            AppShell shell,
            ConfigurationWizardView wizardView,
            LiveDashboardView dashboardView,
            RunSummaryView summaryView,
            UiTheme theme)
        {
            _shell = shell;
            _wizardView = wizardView;
            _dashboardView = dashboardView;
            _summaryView = summaryView;
            _theme = theme;
        }

        public void Initialize()
        {
            // AppShell handles the main render, but we might want to set initial console title etc.
            Console.Title = "Textie v2.0 - Cybernetic Automation";
        }

        public async Task<ConfigurationFlowResult> RunConfigurationWizardAsync(SpamConfiguration current, IReadOnlyList<SpamProfile> profiles, CancellationToken cancellationToken)
        {
            ConfigurationFlowResult result = null!;

            _shell.Render(() =>
            {
                // This call is async, but we are inside a sync Action for Render.
                // We need to just run the wizard logic.
                // Note: The AppShell.Render pattern I designed earlier takes an Action, which is synchronous.
                // This is slightly problematic for Async wizard.
                // I will bypass AppShell.Render for the Wizard flow or run it synchronously (undesirable).
                // Better approach: Use AppShell just for Header/Footer printing manually here.

                // Let's manually invoke Shell for this flow since it's interactive and complex.
                // Or better, let's fix AppShell to be just visual helpers if needed, but for now:
            });

            // Re-render header to be safe
            _shell.Render(() => { });

            // We'll run the wizard "inside" the shell context conceptually.
            // Since `_wizardView.RunAsync` does its own console interactions:
            result = await _wizardView.RunAsync(current, profiles, cancellationToken);

            return result;
        }

        public Task ShowWaitingDashboardAsync(SpamConfiguration configuration, IReadOnlyList<SpamProfile> profiles, CancellationToken cancellationToken)
        {
            _shell.Render(() =>
            {
                _dashboardView.ShowWaitingState(configuration);
            });
            return Task.CompletedTask;
        }

        public async Task<SpamRunSummary?> RunAutomationAsync(SpamConfiguration configuration, TextSpammerEngine engine, Func<CancellationToken, Task<SpamRunSummary?>> runCallback, CancellationToken cancellationToken)
        {
            SpamRunSummary? summary = null;
            Exception? failure = null;

            ProgressTask? progressTask = null;

            void OnProgress(int current, int total)
            {
                if (progressTask != null)
                {
                    progressTask.Value = current;
                    progressTask.Description = $"[cyan]Transmitting[/] {current}/{total}";
                }
            }

            void OnFailure(object? sender, Exception ex)
            {
                failure = ex;
            }

            engine.ProgressChanged += OnProgress;
            engine.SpamFailed += OnFailure;

            try
            {
                // Use AnsiConsole.Live for a cool dashboard?
                // Or standard Progress. The user asked for "Ultrathink" redesign.
                // Let's stick to standard Progress for reliability but styled nicely.

                await AnsiConsole.Progress()
                    .AutoClear(false)
                    .Columns(_dashboardView.GetProgressColumns())
                    .StartAsync(async ctx =>
                    {
                        // We could wrap inside Shell here too?
                        // If we do, the progress bar might conflict if Shell clears screen.
                        // Let's just run progress.

                        progressTask = ctx.AddTask($"[bold {_theme.BrandPrimary.ToMarkup()}]Executing Sequence[/]", maxValue: configuration.Count);
                        summary = await runCallback(cancellationToken).ConfigureAwait(false);

                        if (summary != null)
                        {
                            progressTask.Value = progressTask.MaxValue;
                            progressTask.Description = summary.Cancelled
                                ? $"[bold {_theme.Warning.ToMarkup()}]SEQUENCE ABORTED[/]"
                                : $"[bold {_theme.Success.ToMarkup()}]SEQUENCE COMPLETE[/]";
                        }
                    });
            }
            finally
            {
                engine.ProgressChanged -= OnProgress;
                engine.SpamFailed -= OnFailure;
            }

            if (failure != null) throw failure;
            return summary;
        }

        public void ShowRunSummary(SpamRunSummary summary)
        {
            _shell.Render(() =>
            {
                _summaryView.Show(summary);
            });
        }

        public Task<NextAction> PromptNextActionAsync(CancellationToken cancellationToken)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[{_theme.BrandSecondary.ToMarkup()}]Next Directive[/]")
                    .AddChoices("Re-engage", "Modify Parameters", "Terminate"));

            return Task.FromResult(choice switch
            {
                "Re-engage" => NextAction.RunAgain,
                "Modify Parameters" => NextAction.ChangeSettings,
                _ => NextAction.Exit
            });
        }

        public void ShowError(string message, Exception? exception = null)
        {
            var panel = new Panel($"[{_theme.Danger.ToMarkup()}]{Markup.Escape(message)}[/]")
            {
                Header = new PanelHeader($"[{_theme.BrandPrimary.ToMarkup()} bold]CRITICAL ERROR[/]")
            }
            .BorderColor(_theme.Danger);

            AnsiConsole.Write(panel);
            if (exception != null)
            {
                AnsiConsole.MarkupLine($"[{_theme.MutedText.ToMarkup()}]{Markup.Escape(exception.ToString())}[/]");
            }
        }

        public void Shutdown()
        {
            AnsiConsole.WriteLine();
            var rule = new Rule($"[{_theme.BrandAccent.ToMarkup()}]SESSION TERMINATED[/]") { Style = _theme.PanelBorder };
            AnsiConsole.Write(rule);
        }
    }
