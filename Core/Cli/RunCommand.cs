using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Configuration;
using Textie.Core.Spammer;

namespace Textie.Core.Cli;
    public class RunCommand : AsyncCommand<RunCommand.Settings>
    {
        private readonly ConfigurationManager _configurationManager;
        private readonly TextSpammerEngine _spammerEngine;

        public RunCommand(ConfigurationManager configurationManager, TextSpammerEngine spammerEngine)
        {
            _configurationManager = configurationManager;
            _spammerEngine = spammerEngine;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await _configurationManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            var configuration = await ResolveConfigurationAsync(settings).ConfigureAwait(false);

            if (settings.Message is not null)
            {
                configuration.Message = settings.Message;
            }

            if (settings.Count.HasValue)
            {
                configuration.Count = settings.Count.Value;
            }

            if (settings.Delay.HasValue)
            {
                configuration.DelayMilliseconds = settings.Delay.Value;
            }

            if (!configuration.IsValid())
            {
                AnsiConsole.MarkupLine("[red]Configuration invalid. Aborting.[/]");
                return -1;
            }

            if (settings.Preview)
            {
                AnsiConsole.MarkupLine("[grey62]Preview only. No input will be sent.[/]");
                AnsiConsole.WriteLine(configuration.Message);
                return 0;
            }

            if (settings.FocusDelaySeconds > 0)
            {
                AnsiConsole.MarkupLine($"Focus target window. Starting in {settings.FocusDelaySeconds} second(s)...");
                await Task.Delay(TimeSpan.FromSeconds(settings.FocusDelaySeconds));
            }

            using var cts = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                _spammerEngine.StopSpamming();
            };

            Console.CancelKeyPress += cancelHandler;
            try
            {
                await AnsiConsole.Progress()
                    .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[cyan]Sending messages[/]", maxValue: configuration.Count);
                        void ProgressHandler(int current, int total)
                        {
                            task.Value = current;
                            task.Description = $"[cyan]Sending[/] {current}/{total}";
                        }

                        void FailureHandler(object? sender, Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                        }

                        _spammerEngine.ProgressChanged += ProgressHandler;
                        _spammerEngine.SpamFailed += FailureHandler;
                        try
                        {
                            var result = await _spammerEngine.StartSpammingAsync(configuration, cts.Token).ConfigureAwait(false);
                            task.Value = task.MaxValue;
                            if (result is { Cancelled: true })
                            {
                                AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                            }
                        }
                        finally
                        {
                            _spammerEngine.ProgressChanged -= ProgressHandler;
                            _spammerEngine.SpamFailed -= FailureHandler;
                        }
                    }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled via Ctrl+C[/]");
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }

            return 0;
        }

        private async Task<SpamConfiguration> ResolveConfigurationAsync(Settings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.Profile))
            {
                var profiles = await _configurationManager.GetProfilesAsync(CancellationToken.None).ConfigureAwait(false);
                var profile = profiles.FirstOrDefault(p => string.Equals(p.Name, settings.Profile, StringComparison.OrdinalIgnoreCase));
                if (profile != null)
                {
                    return profile.Configuration.Clone();
                }

                AnsiConsole.MarkupLine($"[yellow]Profile '{Markup.Escape(settings.Profile)}' not found. Using current settings.[/]");
            }

            return _configurationManager.CurrentConfiguration;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("--profile <NAME>")]
            public string? Profile { get; init; }

            [CommandOption("--message <TEXT>")]
            public string? Message { get; init; }

            [CommandOption("--count <VALUE>")]
            public int? Count { get; init; }

            [CommandOption("--delay <MILLISECONDS>")]
            public int? Delay { get; init; }

            [CommandOption("--focus-delay <SECONDS>")]
            public int FocusDelaySeconds { get; init; }

            [CommandOption("--preview")]
            public bool Preview { get; init; }
        }
    }
