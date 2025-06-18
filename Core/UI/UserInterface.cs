using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using Textie.Core.Configuration;

namespace Textie.Core.UI
{
    public class UserInterface
    {
        private const string AppTitle = "TEXTIE";
        private const string AppSubtitle = "Professional Text Automation Tool";

        public void ShowWelcome()
        {
            AnsiConsole.Clear();

            var welcomePanel = new Panel(
                new Markup($"[bold cyan]{AppTitle}[/]\n[italic grey70]{AppSubtitle}[/]\n\n" +
                          "[bold white]Advanced text automation with intelligent control.[/]\n" +
                          "[grey70]Engineered for productivity and precision.[/]"))
                .Header("Welcome")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(2, 1);

            AnsiConsole.Write(welcomePanel);
            AnsiConsole.WriteLine();
        }

        public Task<SpamConfiguration> CollectConfigurationAsync(SpamConfiguration currentConfig)
        {
            var configPanel = new Panel("[bold blue]Configuration Setup[/]")
                .Header("Settings")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(1, 0);

            AnsiConsole.Write(configPanel);
            AnsiConsole.WriteLine();

            var config = new SpamConfiguration();

            // === MESSAGE INPUT WITH ADVANCED VALIDATION ===
            AnsiConsole.MarkupLine("[bold white]Step 1:[/] [cyan]Message Configuration[/]");
            AnsiConsole.WriteLine();

            config.Message = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold green]Enter your message text:[/]")
                    .PromptStyle("green")
                    .DefaultValue(string.IsNullOrEmpty(currentConfig.Message) ? "Hello World" : currentConfig.Message)
                    .DefaultValueStyle("dim")
                    .ValidationErrorMessage("[red]Invalid input - please check the requirements below[/]")
                    .Validate(msg => ValidateMessage(msg)));

            // Show message validation result
            DisplayMessageValidationInfo(config.Message);
            AnsiConsole.WriteLine();

            // === REPETITION COUNT WITH SMART VALIDATION ===
            AnsiConsole.MarkupLine("[bold white]Step 2:[/] [cyan]Repetition Settings[/]");
            AnsiConsole.WriteLine();

            config.Count = AnsiConsole.Prompt(
                new TextPrompt<int>("[bold yellow]Number of repetitions:[/]")
                    .PromptStyle("yellow")
                    .DefaultValue(currentConfig.Count > 0 ? currentConfig.Count : 5)
                    .DefaultValueStyle("dim")
                    .ValidationErrorMessage("[red]Invalid count - please enter a number between 1 and 10,000[/]")
                    .Validate(count => ValidateCount(count)));

            // Show repetition impact
            DisplayCountValidationInfo(config.Count);
            AnsiConsole.WriteLine();

            // === ENHANCED DELAY SELECTION WITH CLEAR FEEDBACK ===
            AnsiConsole.MarkupLine("[bold white]Step 3:[/] [cyan]Timing Configuration[/]");
            AnsiConsole.WriteLine();

            // Show delay selection with descriptions
            var delayChoices = new[]
            {
                "Instant (0ms) - Maximum speed",
                "Rapid (50ms) - Fast automation",
                "Standard (100ms) - Balanced timing",
                "Moderate (500ms) - Deliberate pace",
                "Deliberate (1000ms) - Slow and careful",
                "Custom timing - Specify exact delay"
            };

            var delayChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Select message interval:[/]")
                    .PageSize(7)
                    .MoreChoicesText("[grey50]Navigate with arrow keys[/]")
                    .AddChoices(delayChoices));

            // Parse delay choice and handle custom input
            config.DelayMilliseconds = ExtractDelayFromChoice(delayChoice, currentConfig.DelayMilliseconds);

            // Show timing impact analysis
            DisplayTimingValidationInfo(config);
            AnsiConsole.WriteLine();

            // === CONFIGURATION CONFIRMATION ===
            AnsiConsole.MarkupLine("[bold white]Step 4:[/] [cyan]Review & Confirm[/]");
            AnsiConsole.WriteLine();

            ShowEnhancedConfigurationSummary(config);

            // Final confirmation for large operations
            if (RequiresConfirmation(config))
            {
                var confirmed = AnsiConsole.Confirm(
                    $"[yellow]This will send [bold]{config.Count:N0}[/] messages. Continue?[/]",
                    defaultValue: false);

                if (!confirmed)
                {
                    AnsiConsole.MarkupLine("[yellow]Configuration cancelled. Returning to main menu.[/]");
                    return Task.FromResult(new SpamConfiguration()); // Return empty config to signal cancellation
                }
            }

            return Task.FromResult(config);
        }

        // === VALIDATION HELPERS ===

        private ValidationResult ValidateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return ValidationResult.Error("[red]Message cannot be empty[/]");

            if (message.Length > 1000)
                return ValidationResult.Error("[red]Message too long (maximum 1000 characters)[/]");

            if (message.Length < 1)
                return ValidationResult.Error("[red]Message must contain at least 1 character[/]");

            // Check for potentially problematic characters
            if (message.Contains('\n') || message.Contains('\r'))
                return ValidationResult.Error("[red]Line breaks not supported - use \\n for new lines[/]");

            // Warning for very long messages
            if (message.Length > 500)
                return ValidationResult.Error("[yellow]Warning: Very long message may cause issues in some applications[/]");

            return ValidationResult.Success();
        }

        private ValidationResult ValidateCount(int count)
        {
            if (count < 1)
                return ValidationResult.Error("[red]Count must be at least 1[/]");

            if (count > 10000)
                return ValidationResult.Error("[red]Count cannot exceed 10,000 for safety[/]");

            return ValidationResult.Success();
        }

        private void DisplayMessageValidationInfo(string message)
        {
            var info = new List<string>();

            info.Add($"[green]✓[/] Length: {message.Length} characters");

            if (message.Length > 100)
                info.Add($"[yellow]⚠[/] Long message - may be truncated in some apps");

            if (message.Any(char.IsControl))
                info.Add($"[yellow]⚠[/] Contains special characters");
            else
                info.Add($"[green]✓[/] Standard text characters");

            AnsiConsole.MarkupLine($"  [grey70]{string.Join(" • ", info)}[/]");
        }

        private void DisplayCountValidationInfo(int count)
        {
            var estimatedChars = count.ToString().Length + " characters per count display";
            var riskLevel = count switch
            {
                <= 10 => "[green]Low impact[/]",
                <= 100 => "[yellow]Moderate impact[/]",
                <= 1000 => "[orange3]High impact[/]",
                _ => "[red]Very high impact[/]"
            };

            AnsiConsole.MarkupLine($"  [grey70]Impact assessment: {riskLevel} • Will send {count:N0} messages[/]");
        }

        private int ExtractDelayFromChoice(string choice, int fallbackDelay)
        {
            return choice switch
            {
                string s when s.StartsWith("Instant") => 0,
                string s when s.StartsWith("Rapid") => 50,
                string s when s.StartsWith("Standard") => 100,
                string s when s.StartsWith("Moderate") => 500,
                string s when s.StartsWith("Deliberate") => 1000,
                _ => PromptCustomDelay(fallbackDelay)
            };
        }

        private int PromptCustomDelay(int fallbackDelay)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold cyan]Custom Timing Configuration[/]");

            var customDelay = AnsiConsole.Prompt(
                new TextPrompt<int>("[bold cyan]Enter delay in milliseconds:[/]")
                    .PromptStyle("cyan")
                    .DefaultValue(fallbackDelay >= 0 ? fallbackDelay : 100)
                    .DefaultValueStyle("dim")
                    .ValidationErrorMessage("[red]Please enter a number between 0 and 60,000[/]")
                    .Validate(delay =>
                    {
                        if (delay < 0)
                            return ValidationResult.Error("[red]Delay cannot be negative[/]");
                        if (delay > 60000)
                            return ValidationResult.Error("[red]Delay cannot exceed 60 seconds (60,000ms)[/]");
                        if (delay == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]Warning: Zero delay may overwhelm target application[/]");
                            return ValidationResult.Success();
                        }
                        return ValidationResult.Success();
                    }));

            return customDelay;
        }

        private void DisplayTimingValidationInfo(SpamConfiguration config)
        {
            var totalDuration = TimeSpan.FromMilliseconds(config.Count * config.DelayMilliseconds);
            var messagesPerSecond = config.DelayMilliseconds > 0 ? Math.Round(1000.0 / config.DelayMilliseconds, 1) : double.PositiveInfinity;

            var speedAssessment = config.DelayMilliseconds switch
            {
                0 => "[red]Maximum speed - may overwhelm applications[/]",
                < 100 => "[orange3]Very fast - use with caution[/]",
                < 500 => "[yellow]Fast - good for most applications[/]",
                < 1000 => "[green]Moderate - safe for all applications[/]",
                _ => "[cyan]Slow - very safe timing[/]"
            };

            var rate = messagesPerSecond == double.PositiveInfinity ? "Unlimited" : $"{messagesPerSecond:F1}/sec";

            AnsiConsole.MarkupLine($"  [grey70]Speed: {rate} • Duration: {totalDuration:mm\\:ss} • {speedAssessment}[/]");
        }

        private bool RequiresConfirmation(SpamConfiguration config)
        {
            // Require confirmation for large operations or very fast speeds
            return config.Count >= 100 ||
                   config.DelayMilliseconds <= 10 ||
                   (config.Count * config.DelayMilliseconds) > 300000; // > 5 minutes
        }

        private void ShowEnhancedConfigurationSummary(SpamConfiguration config)
        {
            var summaryTable = new Table()
                .AddColumn(new TableColumn("[bold]Parameter[/]").Width(15))
                .AddColumn(new TableColumn("[bold]Value[/]").Width(25))
                .AddColumn(new TableColumn("[bold]Assessment[/]").Width(25))
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Green);

            // Message row
            var messagePreview = config.Message.Length > 20 ? config.Message[..17] + "..." : config.Message;
            var messageAssessment = config.Message.Length switch
            {
                <= 50 => "[green]Optimal length[/]",
                <= 200 => "[yellow]Long message[/]",
                _ => "[orange3]Very long[/]"
            };
            summaryTable.AddRow("[cyan]Message[/]", $"[white]\"{messagePreview}\"[/]", messageAssessment);

            // Count row
            var countAssessment = config.Count switch
            {
                <= 10 => "[green]Small batch[/]",
                <= 100 => "[yellow]Medium batch[/]",
                <= 1000 => "[orange3]Large batch[/]",
                _ => "[red]Very large batch[/]"
            };
            summaryTable.AddRow("[cyan]Repetitions[/]", $"[white]{config.Count:N0}[/]", countAssessment);

            // Timing row
            var timingAssessment = config.DelayMilliseconds switch
            {
                0 => "[red]Instant[/]",
                <= 50 => "[orange3]Very fast[/]",
                <= 200 => "[yellow]Fast[/]",
                <= 500 => "[green]Moderate[/]",
                _ => "[cyan]Slow[/]"
            };
            summaryTable.AddRow("[cyan]Interval[/]", $"[white]{config.DelayMilliseconds}ms[/]", timingAssessment);

            // Duration row
            var totalDuration = TimeSpan.FromMilliseconds(config.Count * config.DelayMilliseconds);
            var durationAssessment = totalDuration.TotalSeconds switch
            {
                <= 10 => "[green]Quick[/]",
                <= 60 => "[yellow]Short[/]",
                <= 300 => "[orange3]Medium[/]",
                _ => "[red]Long operation[/]"
            };
            summaryTable.AddRow("[cyan]Duration[/]", $"[white]{totalDuration:mm\\:ss}[/]", durationAssessment);

            var summaryPanel = new Panel(summaryTable)
                .Header("Configuration Review")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green);

            AnsiConsole.Write(summaryPanel);
            AnsiConsole.WriteLine();
        }

        public void ShowInstructions(SpamConfiguration config)
        {
            var instructions = new Markup(
                $"[bold]Operation Instructions:[/]\n\n" +
                $"[bold green]• Press [yellow]ENTER[/] to begin automation[/]\n" +
                $"[bold red]• Press [yellow]ESCAPE[/] to halt operation[/]\n" +
                $"[bold blue]• Ensure target application has [underline]focus[/][/]\n\n" +
                $"[bold cyan]Active Configuration:[/]\n" +
                $"[grey70]Text: \"{config.Message}\"[/]\n" +
                $"[grey70]Count: {config.Count:N0} • Interval: {config.DelayMilliseconds}ms[/]");

            var instructionPanel = new Panel(instructions)
                .Header("Ready")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Yellow)
                .Padding(2, 1);

            AnsiConsole.Write(instructionPanel);
            AnsiConsole.WriteLine();
        }

        public void ShowWaitingStatus()
        {
            AnsiConsole.MarkupLine("[bold cyan]Monitoring input...[/] [grey70](Press ENTER to start)[/]");
            AnsiConsole.WriteLine();
        }

        public void ShowSpamStarted()
        {
            AnsiConsole.MarkupLine("[bold green]Automation initiated[/] [grey70](Press ESCAPE to stop)[/]");
        }

        public void ShowSpamCompleted()
        {
            AnsiConsole.MarkupLine("[bold green]Operation completed successfully[/]");
        }

        public void ShowSpamCancelled()
        {
            AnsiConsole.MarkupLine("[bold yellow]Operation cancelled by user[/]");
        }

        public NextAction GetNextAction()
        {
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold blue]Select next action:[/]")
                    .PageSize(4)
                    .AddChoices(new[]
                    {
                        "Repeat with current settings",
                        "Modify configuration",
                        "Exit application"
                    }));

            return choice switch
            {
                "Repeat with current settings" => NextAction.RunAgain,
                "Modify configuration" => NextAction.ChangeSettings,
                _ => NextAction.Exit
            };
        }

        public void ShowError(string message)
        {
            var errorPanel = new Panel($"[bold red]{message}[/]")
                .Header("Error")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red);

            AnsiConsole.Write(errorPanel);
            AnsiConsole.WriteLine();
        }

        public void ShowGoodbye()
        {
            var goodbyePanel = new Panel(
                "[bold cyan]Thank you for using Textie[/]\n" +
                "[grey70]Professional automation tools[/]")
                .Header("Session Complete")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0);

            AnsiConsole.Write(goodbyePanel);
        }
    }

    public enum NextAction
    {
        RunAgain,
        ChangeSettings,
        Exit
    }
}
