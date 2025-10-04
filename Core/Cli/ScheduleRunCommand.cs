using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Configuration;
using Textie.Core.Scheduling;
using Textie.Core.Spammer;

namespace Textie.Core.Cli
{
    public class ScheduleRunCommand : AsyncCommand
    {
        private readonly ConfigurationManager _configurationManager;
        private readonly ScheduleManager _scheduleManager;
        private readonly TextSpammerEngine _spammerEngine;

        public ScheduleRunCommand(ConfigurationManager configurationManager, ScheduleManager scheduleManager, TextSpammerEngine spammerEngine)
        {
            _configurationManager = configurationManager;
            _scheduleManager = scheduleManager;
            _spammerEngine = spammerEngine;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            await _configurationManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            await _scheduleManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            var schedules = await _scheduleManager.GetSchedulesAsync(CancellationToken.None).ConfigureAwait(false);
            var profiles = await _configurationManager.GetProfilesAsync(CancellationToken.None).ConfigureAwait(false);
            var dueSchedules = schedules.Where(s => s.Enabled && (!s.NextRun.HasValue || s.NextRun <= DateTimeOffset.Now)).ToList();

            if (!dueSchedules.Any())
            {
                AnsiConsole.MarkupLine("[grey62]No schedules are due.[/]");
                return 0;
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                _spammerEngine.StopSpamming();
            };

            foreach (var schedule in dueSchedules)
            {
                if (cts.IsCancellationRequested)
                {
                    break;
                }

                var profile = profiles.FirstOrDefault(p => string.Equals(p.Name, schedule.ProfileName, StringComparison.OrdinalIgnoreCase));
                if (profile == null)
                {
                    AnsiConsole.MarkupLine($"[red]Profile '{schedule.ProfileName}' not found for schedule '{schedule.Name}'.[/]");
                    continue;
                }

                AnsiConsole.MarkupLine($"[cyan]Running schedule[/] {schedule.Name} -> profile '{schedule.ProfileName}'.");
                try
                {
                    await AnsiConsole.Progress()
                        .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
                        .StartAsync(async ctx =>
                        {
                            var task = ctx.AddTask($"[cyan]{schedule.Name}[/]", maxValue: profile.Configuration.Count);
                            void ProgressHandler(object? sender, SpamProgressEventArgs args)
                            {
                                task.Value = args.Current;
                                task.Description = $"[cyan]{schedule.Name}[/] {args.Current}/{args.Total}";
                            }

                            _spammerEngine.ProgressChanged += ProgressHandler;
                            try
                            {
                                await _spammerEngine.StartSpammingAsync(profile.Configuration.Clone(), cts.Token).ConfigureAwait(false);
                                task.Value = task.MaxValue;
                            }
                            finally
                            {
                                _spammerEngine.ProgressChanged -= ProgressHandler;
                            }
                        }).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    AnsiConsole.MarkupLine("[yellow]Schedule execution cancelled.[/]");
                    break;
                }

                await _configurationManager.SaveProfileAsync(new SpamProfile
                {
                    Name = profile.Name,
                    Notes = profile.Notes,
                    Configuration = profile.Configuration.Clone()
                }, CancellationToken.None).ConfigureAwait(false);

                var runAt = DateTimeOffset.Now;
                schedule.LastRun = runAt;
                schedule.NextRun = ScheduleManager.ComputeNextRun(schedule.CronExpression, runAt);
                await _scheduleManager.AddOrUpdateAsync(schedule, CancellationToken.None).ConfigureAwait(false);
                profiles = await _configurationManager.GetProfilesAsync(CancellationToken.None).ConfigureAwait(false);
            }

            return 0;
        }
    }
}
