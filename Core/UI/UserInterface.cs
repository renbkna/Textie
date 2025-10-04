using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Textie.Core.Configuration;
using Textie.Core.Spammer;

namespace Textie.Core.UI
{
    public class UserInterface : IUserInterface
    {
        private readonly UiTheme _theme = new();

        public void Initialize()
        {
            AnsiConsole.Clear();
            var header = new FigletText("TEXTIE")
                .Color(_theme.BrandPrimary)
                .Centered();
            AnsiConsole.Write(header);
            AnsiConsole.Write(new Rule("[grey53]Text Automation[/]").Centered());
            AnsiConsole.WriteLine();
        }

        public Task<ConfigurationFlowResult> RunConfigurationWizardAsync(SpamConfiguration current, IReadOnlyList<SpamProfile> profiles, CancellationToken cancellationToken)
        {
            var working = current.Clone();
            SpamProfile? selectedProfile = null;
            bool saveProfile = false;
            string? profileName = null;

            if (profiles.Count > 0)
            {
                var profileChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Start from[/]")
                        .AddChoices("Current settings", "Select profile", "Reset to defaults", "Cancel setup"));

                if (profileChoice == "Select profile")
                {
                    selectedProfile = PromptForProfile(profiles);
                    if (selectedProfile != null)
                    {
                        working = selectedProfile.Configuration.Clone();
                    }
                }
                else if (profileChoice == "Reset to defaults")
                {
                    working = new SpamConfiguration();
                }
                else if (profileChoice == "Cancel setup")
                {
                    return Task.FromResult(new ConfigurationFlowResult(true, current, null, false, null));
                }
            }

            int stepIndex = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RenderStepper(stepIndex, working, selectedProfile);

                var nav = stepIndex switch
                {
                    0 => ConfigureMessage(working),
                    1 => ConfigureCount(working),
                    2 => ConfigureDelay(working),
                    3 => ConfigureStrategy(working),
                    4 => ConfigureAdvanced(working),
                    5 => ReviewConfiguration(working, out saveProfile, out profileName),
                    _ => WizardNavigation.Complete
                };

                if (nav == WizardNavigation.Cancel)
                {
                    return Task.FromResult(new ConfigurationFlowResult(true, current, null, false, null));
                }

                if (nav == WizardNavigation.Back)
                {
                    stepIndex = Math.Max(0, stepIndex - 1);
                    continue;
                }

                stepIndex++;

                if (stepIndex > 5)
                {
                    break;
                }
            }

            return Task.FromResult(new ConfigurationFlowResult(false, working.Clone(), selectedProfile, saveProfile, profileName));
        }

        public Task ShowWaitingDashboardAsync(SpamConfiguration configuration, IReadOnlyList<SpamProfile> profiles, CancellationToken cancellationToken)
        {
            var layout = new Layout("root")
                .SplitRows(
                    new Layout("instructions").Size(8),
                    new Layout("details"));

            layout["instructions"].Update(BuildInstructionPanel(configuration));

            var detailLayout = new Layout("detail-root")
                .SplitColumns(
                    new Layout("config").Ratio(2),
                    new Layout("profiles").Ratio(1));

            detailLayout["config"].Update(BuildConfigurationSummary(configuration));
            detailLayout["profiles"].Update(BuildProfilesPanel(profiles));

            layout["details"].Update(detailLayout);

            AnsiConsole.Write(layout);
            AnsiConsole.WriteLine();
            return Task.CompletedTask;
        }

        public async Task<SpamRunSummary?> RunAutomationAsync(SpamConfiguration configuration, TextSpammerEngine engine, Func<CancellationToken, Task<SpamRunSummary?>> runCallback, CancellationToken cancellationToken)
        {
            SpamRunSummary? summary = null;
            Exception? failure = null;

            ProgressTask? progressTask = null;

            void OnProgress(object? sender, SpamProgressEventArgs e)
            {
                if (progressTask != null)
                {
                    progressTask.Value = e.Current;
                    progressTask.Description = $"[cyan]Sending[/] {e.Current}/{e.Total} [{e.Status}]";
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
                await AnsiConsole.Progress()
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new ElapsedTimeColumn(),
                        new RemainingTimeColumn(),
                        new SpinnerColumn())
                    .StartAsync(async ctx =>
                    {
                        progressTask = ctx.AddTask("[cyan]Preparing automation[/]", maxValue: configuration.Count);
                        summary = await runCallback(cancellationToken).ConfigureAwait(false);
                        if (summary != null)
                        {
                            progressTask.Value = progressTask.MaxValue;
                            progressTask.Description = summary.Cancelled
                                ? "[yellow]Operation cancelled[/]"
                                : "[green]Automation completed[/]";
                        }
                    });
            }
            finally
            {
                engine.ProgressChanged -= OnProgress;
                engine.SpamFailed -= OnFailure;
            }

            if (failure != null)
            {
                throw failure;
            }

            return summary;
        }

        public void ShowRunSummary(SpamRunSummary summary)
        {
            var table = new Table()
                .AddColumn("[bold]Metric[/]")
                .AddColumn("[bold]Value[/]")
                .Border(TableBorder.Rounded)
                .BorderColor(_theme.BrandAccent);

            table.AddRow("Messages sent", summary.MessagesSent.ToString());
            table.AddRow("Duration", summary.Duration == TimeSpan.Zero ? "Instant" : summary.Duration.ToString("mm':'ss"));
            var status = summary.FocusLost
                ? "[red]Stopped (focus lost)[/]"
                : summary.Cancelled ? "[yellow]Cancelled[/]" : "[green]Completed[/]";
            table.AddRow("Status", status);
            table.AddRow("Errors", summary.Errors == 0 ? "[green]0[/]" : $"[red]{summary.Errors}[/]");

            var panel = new Panel(table)
                .Header("Run summary")
                .BorderColor(_theme.BrandPrimary)
                .Padding(1, 0);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        public Task<NextAction> PromptNextActionAsync(CancellationToken cancellationToken)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Next action[/]")
                    .AddChoices("Run again", "Modify configuration", "Exit"));

            var next = choice switch
            {
                "Run again" => NextAction.RunAgain,
                "Modify configuration" => NextAction.ChangeSettings,
                _ => NextAction.Exit
            };

            return Task.FromResult(next);
        }

        public void ShowError(string message, Exception? exception = null)
        {
            var panel = new Panel($"[red]{message}[/]")
                .BorderColor(_theme.Danger)
                .Header("Error");
            AnsiConsole.Write(panel);
            if (exception != null)
            {
                AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
            }
        }

        public void Shutdown()
        {
            var goodbye = new Panel("[cyan]Thank you for using Textie[/]\n[grey]Stay productive![/]")
                .BorderColor(_theme.BrandPrimary)
                .Header("Session Complete");
            AnsiConsole.Write(goodbye);
        }

        private SpamProfile? PromptForProfile(IReadOnlyList<SpamProfile> profiles)
        {
            var profileName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select a profile[/]")
                    .AddChoices(profiles.Select(p => p.Name)));

            return profiles.FirstOrDefault(p => p.Name == profileName);
        }

        private WizardNavigation ConfigureMessage(SpamConfiguration working)
        {
            var messagePrompt = new TextPrompt<string>("[bold green]Message text[/]")
                .DefaultValue(string.IsNullOrWhiteSpace(working.Message) ? "Hello World" : working.Message)
                .AllowEmpty();

            var message = AnsiConsole.Prompt(messagePrompt);
            if (string.IsNullOrWhiteSpace(message))
            {
                var confirmCancel = AnsiConsole.Confirm("Message is empty. Cancel setup?", false);
                if (confirmCancel)
                {
                    return WizardNavigation.Cancel;
                }
                message = working.Message;
            }

            if (message.Length > 500)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Very long message ({message.Length} chars) may be truncated by some applications.");
            }

            working.Message = message;
            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureCount(SpamConfiguration working)
        {
            var count = AnsiConsole.Prompt(
                new TextPrompt<int>("[bold yellow]Repetition count[/]")
                    .DefaultValue(working.Count)
                    .ValidationErrorMessage("[red]Enter a value between 1 and 10,000[/]")
                    .Validate(value => value is >= 1 and <= 10000 ? ValidationResult.Success() : ValidationResult.Error("Invalid count")));

            working.Count = count;
            ShowImpact("Count", count switch
            {
                <= 10 => "[green]Low impact[/]",
                <= 100 => "[yellow]Medium impact[/]",
                <= 1000 => "[orange3]High impact[/]",
                _ => "[red]Very high impact[/]"
            });

            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureDelay(SpamConfiguration working)
        {
            var choices = new[]
            {
                "Instant (0ms) - Maximum speed",
                "Rapid (50ms)",
                "Standard (100ms)",
                "Measured (250ms)",
                "Moderate (500ms)",
                "Deliberate (1000ms)",
                "Custom"
            };

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Select delay between messages[/]")
                    .AddChoices(choices));

            if (selection == "Custom")
            {
                working.DelayMilliseconds = AnsiConsole.Prompt(
                    new TextPrompt<int>("[bold cyan]Delay (ms)[/]")
                        .DefaultValue(working.DelayMilliseconds)
                        .ValidationErrorMessage("[red]0 to 120,000 only[/]")
                        .Validate(value => value is >= 0 and <= 120000 ? ValidationResult.Success() : ValidationResult.Error("Invalid delay")));
            }
            else
            {
                working.DelayMilliseconds = selection switch
                {
                    var s when s.StartsWith("Instant") => 0,
                    var s when s.StartsWith("Rapid") => 50,
                    var s when s.StartsWith("Standard") => 100,
                    var s when s.StartsWith("Measured") => 250,
                    var s when s.StartsWith("Moderate") => 500,
                    _ => 1000
                };
            }

            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureStrategy(SpamConfiguration working)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select automation strategy[/]")
                    .AddChoices(
                        "Send text and press Enter",
                        "Send text only",
                        "Type per character"));

            working.Strategy = choice switch
            {
                "Send text only" => SpamStrategy.SendTextOnly,
                "Type per character" => SpamStrategy.TypePerCharacter,
                _ => SpamStrategy.SendTextAndEnter
            };

            working.SendSubmitKey = working.Strategy != SpamStrategy.SendTextOnly || AnsiConsole.Confirm("Press Enter after message?", working.SendSubmitKey);
            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureAdvanced(SpamConfiguration working)
        {
            working.DelayJitterPercent = AnsiConsole.Prompt(
                new TextPrompt<int>("[bold]Delay jitter (%)[/]")
                    .DefaultValue(working.DelayJitterPercent)
                    .ValidationErrorMessage("[red]0 to 100[/]")
                    .Validate(value => value is >= 0 and <= 100 ? ValidationResult.Success() : ValidationResult.Error("Invalid jitter")));

            if (working.Strategy == SpamStrategy.TypePerCharacter)
            {
                working.PerCharacterDelayMilliseconds = AnsiConsole.Prompt(
                    new TextPrompt<int>("[bold]Per-character delay (ms)[/]")
                        .DefaultValue(working.PerCharacterDelayMilliseconds)
                        .ValidationErrorMessage("[red]0 to 500[/]")
                        .Validate(value => value is >= 0 and <= 500 ? ValidationResult.Success() : ValidationResult.Error("Invalid delay")));
            }

            working.EnableTemplating = AnsiConsole.Confirm("Enable templating (supports {index}, {timestamp}, {guid})?", working.EnableTemplating);

            working.LockTargetWindow = AnsiConsole.Confirm("Lock to target window (warn if focus changes)?", working.LockTargetWindow);
            if (working.LockTargetWindow)
            {
                working.TargetWindowTitle = AnsiConsole.Ask<string>("Target window title contains?", working.TargetWindowTitle ?? string.Empty);
            }
            else
            {
                working.TargetWindowTitle = null;
            }

            return WizardNavigation.Next;
        }

        private WizardNavigation ReviewConfiguration(SpamConfiguration working, out bool saveProfile, out string? profileName)
        {
            saveProfile = false;
            profileName = null;

            var summary = BuildConfigurationSummary(working);
            AnsiConsole.Write(summary);

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Confirm configuration[/]")
                    .AddChoices("Start automation", "Save as profile", "Back", "Cancel"));

            if (action == "Save as profile")
            {
                profileName = AnsiConsole.Ask<string>("Profile name", profileName ?? "My profile");
                saveProfile = true;
                return WizardNavigation.Next;
            }

            if (action == "Back")
            {
                return WizardNavigation.Back;
            }

            if (action == "Cancel")
            {
                return WizardNavigation.Cancel;
            }

            return WizardNavigation.Next;
        }

        private static void ShowImpact(string category, string impact)
        {
            AnsiConsole.MarkupLine($"  [grey62]{category} impact: {impact}[/]");
        }

        private Panel BuildInstructionPanel(SpamConfiguration config)
        {
            var markup = new Markup(
                "[bold]Instructions[/]\n" +
                "• Focus the target application\n" +
                "• Press [green]ENTER[/] to start\n" +
                "• Press [yellow]ESC[/] to cancel\n\n" +
                $"Strategy: [cyan]{config.Strategy}[/]" +
                (config.LockTargetWindow ? "\nFocus lock enabled" : string.Empty));

            return new Panel(markup)
                .BorderColor(_theme.BrandPrimary)
                .Header("Ready");
        }

        private Panel BuildConfigurationSummary(SpamConfiguration config)
        {
            var table = new Table()
                .AddColumn("[bold]Parameter[/]")
                .AddColumn("[bold]Value[/]")
                .AddColumn("[bold]Assessment[/]")
                .Border(TableBorder.Rounded)
                .BorderColor(_theme.BrandAccent);

            var messagePreview = config.Message.Length > 30
                ? config.Message[..27] + "..."
                : config.Message;

            table.AddRow("Message", $"[white]{messagePreview}[/]", config.Message.Length switch
            {
                <= 50 => "[green]Optimal length[/]",
                <= 200 => "[yellow]Long[/]",
                _ => "[orange3]Very long[/]"
            });

            table.AddRow("Count", $"{config.Count:N0}", config.Count switch
            {
                <= 10 => "[green]Small batch[/]",
                <= 100 => "[yellow]Medium[/]",
                <= 1000 => "[orange3]Large[/]",
                _ => "[red]Very large[/]"
            });

            table.AddRow("Delay", $"{config.DelayMilliseconds} ms", config.DelayMilliseconds switch
            {
                0 => "[red]Instant[/]",
                <= 50 => "[orange3]Very fast[/]",
                <= 200 => "[yellow]Fast[/]",
                <= 500 => "[green]Balanced[/]",
                _ => "[cyan]Conservative[/]"
            });

            table.AddRow("Strategy", config.Strategy.ToString(), config.Strategy switch
            {
                SpamStrategy.SendTextAndEnter => "[green]Standard[/]",
                SpamStrategy.SendTextOnly => "[yellow]Custom[/]",
                SpamStrategy.TypePerCharacter => "[cyan]Humanized[/]",
                _ => ""
            });

            table.AddRow("Jitter", $"{config.DelayJitterPercent}%", config.DelayJitterPercent switch
            {
                0 => "[green]Stable[/]",
                <= 15 => "[yellow]Light variation[/]",
                _ => "[orange3]High variation[/]"
            });

            table.AddRow("Templating", config.EnableTemplating ? "Enabled" : "Disabled", config.EnableTemplating ? "[cyan]Dynamic[/]" : "[grey66]Static[/]");

            if (config.LockTargetWindow && !string.IsNullOrWhiteSpace(config.TargetWindowTitle))
            {
                table.AddRow("Target", config.TargetWindowTitle, "[cyan]Locked[/]");
            }

            return new Panel(table)
                .BorderColor(_theme.BrandAccent)
                .Header("Configuration Summary");
        }

        private Panel BuildProfilesPanel(IReadOnlyList<SpamProfile> profiles)
        {
            if (profiles.Count == 0)
            {
                return new Panel("[grey62]No profiles saved yet[/]")
                    .BorderColor(_theme.Neutral)
                    .Header("Profiles");
            }

            var table = new Table()
                .AddColumn("[bold]Profile[/]")
                .AddColumn("[bold]Last used[/]")
                .Border(TableBorder.Rounded)
                .BorderColor(_theme.BrandAccent);

            foreach (var profile in profiles.OrderByDescending(p => p.LastUsed).Take(5))
            {
                table.AddRow(profile.Name, profile.LastUsed.ToLocalTime().ToString("g"));
            }

            return new Panel(table)
                .BorderColor(_theme.BrandAccent)
                .Header("Profiles");
        }

        private void RenderStepper(int stepIndex, SpamConfiguration working, SpamProfile? profile)
        {
            AnsiConsole.Clear();
            Initialize();

            if (profile != null)
            {
                AnsiConsole.MarkupLine($"Using profile: [cyan]{profile.Name}[/]\n");
            }

            var steps = new[]
            {
                "Message",
                "Count",
                "Delay",
                "Strategy",
                "Advanced",
                "Review"
            };

            var stepTable = new Table()
                .AddColumn(new TableColumn("[bold]Step[/]").NoWrap())
                .HideHeaders()
                .Border(TableBorder.None);

            foreach (var (step, index) in steps.Select((s, idx) => (s, idx)))
            {
                var state = index < stepIndex ? "[green]✔" : index == stepIndex ? "[cyan]➤" : "[grey50]•";
                stepTable.AddRow($"{state}[/] {step}");
            }

            AnsiConsole.Write(stepTable);
            AnsiConsole.WriteLine();
            AnsiConsole.Write(BuildConfigurationSummary(working));
            AnsiConsole.WriteLine();
        }

        private enum WizardNavigation
        {
            Next,
            Back,
            Cancel,
            Complete
        }
    }
}
