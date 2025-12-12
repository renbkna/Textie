using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Textie.Core.Configuration;
using Textie.Core.Spammer;
using Textie.Core.UI;

namespace Textie.Core.UI.Views;
    public class ConfigurationWizardView
    {
        private readonly UiTheme _theme;
        private readonly ConfigurationSummaryView _summaryView;

        public ConfigurationWizardView(UiTheme theme, ConfigurationSummaryView summaryView)
        {
            _theme = theme;
            _summaryView = summaryView;
        }

        public Task<ConfigurationFlowResult> RunAsync(SpamConfiguration current, IReadOnlyList<SpamProfile> profiles, CancellationToken cancellationToken)
        {
            var working = current.Clone();
            SpamProfile? selectedProfile = null;
            bool saveProfile = false;
            string? profileName = null;

            // Step 0: Profile Selection (Optional)
            if (profiles.Count > 0)
            {
                var choice = PromptProfileSelection();
                if (choice == "Select profile")
                {
                    selectedProfile = PromptForProfile(profiles);
                    if (selectedProfile != null) working = selectedProfile.Configuration.Clone();
                }
                else if (choice == "Reset to defaults")
                {
                    working = new SpamConfiguration();
                }
                else if (choice == "Cancel setup")
                {
                    return Task.FromResult(new ConfigurationFlowResult(true, current, null, false, null));
                }
            }

            int stepIndex = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RenderContext(stepIndex, working, selectedProfile);

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

                if (nav == WizardNavigation.Cancel) return Task.FromResult(new ConfigurationFlowResult(true, current, null, false, null));
                if (nav == WizardNavigation.Back)
                {
                    stepIndex = Math.Max(0, stepIndex - 1);
                    continue;
                }

                stepIndex++;
                if (stepIndex > 5) break;
            }

            return Task.FromResult(new ConfigurationFlowResult(false, working.Clone(), selectedProfile, saveProfile, profileName));
        }

        private void RenderContext(int stepIndex, SpamConfiguration working, SpamProfile? profile)
        {
            AnsiConsole.Clear();

            var grid = new Grid();
            grid.AddColumn(new GridColumn().PadRight(4));
            grid.AddColumn(new GridColumn());

            grid.AddRow(
                GetStepsPanel(stepIndex),
                _summaryView.GetPanel(working)
            );

            AnsiConsole.Write(grid);
            AnsiConsole.WriteLine();
        }

        private Panel GetStepsPanel(int currentStep)
        {
            var steps = new[] { "Message", "Count", "Delay", "Strategy", "Advanced", "Review" };
            var grid = new Grid();
            grid.AddColumn(new GridColumn());

            for (int i = 0; i < steps.Length; i++)
            {
                var prefix = i < currentStep ? "✔" : i == currentStep ? "➤" : "•";
                var color = i < currentStep ? _theme.StepComplete : i == currentStep ? _theme.StepCurrent : _theme.StepPending;

                var style = i == currentStep ? _theme.Highlight : (i < currentStep ? _theme.NormalText : _theme.MutedText);
                var labelMarkup = i == currentStep ? $"[bold]{steps[i]}[/]" : steps[i];

                grid.AddRow($"[{color.ToMarkup()}]{prefix}[/] {labelMarkup}");
            }

            return new Panel(grid)
            {
                Header = new PanelHeader($"[{_theme.BrandPrimary.ToMarkup()} bold]Progress[/]")
            }
            .BorderColor(_theme.Neutral);
        }

        // --- Step Implementations ---

        private string PromptProfileSelection()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[{_theme.BrandSecondary.ToMarkup()} bold]Start from[/]")
                    .AddChoices("Current settings", "Select profile", "Reset to defaults", "Cancel setup"));
        }

        private SpamProfile? PromptForProfile(IReadOnlyList<SpamProfile> profiles)
        {
            var name = AnsiConsole.Prompt(
               new SelectionPrompt<string>()
                   .Title($"[{_theme.BrandSecondary.ToMarkup()} bold]Select a profile[/]")
                   .AddChoices(profiles.Select(p => p.Name)));
            return profiles.FirstOrDefault(p => p.Name == name);
        }

        private WizardNavigation ConfigureMessage(SpamConfiguration working)
        {
            var prompt = new TextPrompt<string>($"[{_theme.Success.ToMarkup()} bold]Message payload[/]")
                .DefaultValue(string.IsNullOrWhiteSpace(working.Message) ? "Hello World" : working.Message)
                .AllowEmpty();

            var msg = AnsiConsole.Prompt(prompt);
            if (string.IsNullOrWhiteSpace(msg))
            {
                if (AnsiConsole.Confirm("Message is empty. Cancel setup?", false)) return WizardNavigation.Cancel;
                msg = working.Message;
            }
            working.Message = msg;
            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureCount(SpamConfiguration working)
        {
            working.Count = AnsiConsole.Prompt(
                new TextPrompt<int>($"[{_theme.Warning.ToMarkup()} bold]Repetition count[/]")
                    .DefaultValue(working.Count)
                    .Validate(v => v is >= 1 and <= 10000 ? ValidationResult.Success() : ValidationResult.Error("1-10,000 only")));
            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureDelay(SpamConfiguration working)
        {
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<DelayOption>()
                    .Title($"[{_theme.BrandPrimary.ToMarkup()} bold]Delay[/]")
                    .AddChoices(DelayOption.Presets)
                    .UseConverter(opt => opt.Label));

            if (selection.IsCustom)
            {
                working.DelayMilliseconds = AnsiConsole.Prompt(
                    new TextPrompt<int>($"[{_theme.BrandAccent.ToMarkup()}]Custom Delay (ms)[/]")
                        .DefaultValue(working.DelayMilliseconds)
                        .Validate(v => v is >= 0 and <= 120000 ? ValidationResult.Success() : ValidationResult.Error("Max 120s")));
            }
            else
            {
                working.DelayMilliseconds = selection.Milliseconds;
            }

            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureStrategy(SpamConfiguration working)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[{_theme.BrandSecondary.ToMarkup()} bold]Strategy[/]")
                    .AddChoices("Send text and press Enter", "Send text only", "Type per character"));

            working.Strategy = choice switch
            {
                "Send text only" => SpamStrategy.SendTextOnly,
                "Type per character" => SpamStrategy.TypePerCharacter,
                _ => SpamStrategy.SendTextAndEnter
            };

            working.SendSubmitKey = working.Strategy == SpamStrategy.SendTextAndEnter
                || AnsiConsole.Confirm("Press Enter after message?", working.SendSubmitKey);
            return WizardNavigation.Next;
        }

        private WizardNavigation ConfigureAdvanced(SpamConfiguration working)
        {
            working.DelayJitterPercent = AnsiConsole.Prompt(
                new TextPrompt<int>($"[{_theme.Neutral.ToMarkup()}]Jitter (%)[/]")
                    .DefaultValue(working.DelayJitterPercent)
                    .Validate(v => v is >= 0 and <= 100 ? ValidationResult.Success() : ValidationResult.Error("0-100")));

            if (working.Strategy == SpamStrategy.TypePerCharacter)
            {
                working.PerCharacterDelayMilliseconds = AnsiConsole.Prompt(
                    new TextPrompt<int>($"[{_theme.Neutral.ToMarkup()}]Per-char Delay (ms)[/]")
                        .DefaultValue(working.PerCharacterDelayMilliseconds)
                        .Validate(v => v >= 0 && v <= 500));
            }

            working.EnableTemplating = AnsiConsole.Confirm("Enable Templating?", working.EnableTemplating);
            working.LockTargetWindow = AnsiConsole.Confirm("Lock to Window Target?", working.LockTargetWindow);
            if (working.LockTargetWindow)
            {
                working.TargetWindowTitle = AnsiConsole.Ask<string>("Target Title Contains?", working.TargetWindowTitle ?? "");
            }
            else working.TargetWindowTitle = null;

            return WizardNavigation.Next;
        }

        private WizardNavigation ReviewConfiguration(SpamConfiguration working, out bool saveProfile, out string? profileName)
        {
            saveProfile = false;
            profileName = null;

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[{_theme.Success.ToMarkup()} bold]Ready to Launch[/]")
                    .AddChoices("Start automation", "Save as profile", "Back", "Cancel"));

            if (action == "Save as profile")
            {
                profileName = AnsiConsole.Ask<string>("Profile Name", "My Profile");
                saveProfile = true;
                return WizardNavigation.Next;
            }

            return action switch
            {
                "Back" => WizardNavigation.Back,
                "Cancel" => WizardNavigation.Cancel,
                _ => WizardNavigation.Next
            };
        }
    }
