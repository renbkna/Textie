using System;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Scheduling;

namespace Textie.Core.Cli
{
    public class ScheduleAddCommand : AsyncCommand<ScheduleAddCommand.Settings>
    {
        private readonly ScheduleManager _scheduleManager;

        public ScheduleAddCommand(ScheduleManager scheduleManager)
        {
            _scheduleManager = scheduleManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Name) || string.IsNullOrWhiteSpace(settings.Profile))
            {
                AnsiConsole.MarkupLine("[red]Name and profile required.[/]");
                return -1;
            }

            await _scheduleManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            var nextRun = ScheduleManager.ComputeNextRun(settings.Cron, DateTimeOffset.Now);
            var scheduledRun = new ScheduledRun
            {
                Name = settings.Name,
                ProfileName = settings.Profile,
                CronExpression = settings.Cron,
                Enabled = !settings.Disabled,
                NextRun = nextRun
            };

            await _scheduleManager.AddOrUpdateAsync(scheduledRun, CancellationToken.None).ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[green]Schedule '{settings.Name}' added. Next run: {nextRun:g}[/]");
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("--name <NAME>")]
            public string Name { get; init; } = string.Empty;

            [CommandOption("--profile <PROFILE>")]
            public string Profile { get; init; } = string.Empty;

            [CommandOption("--cron <EXPRESSION>")]
            public string Cron { get; init; } = "0 0 * * *";

            [CommandOption("--disabled")]
            public bool Disabled { get; init; }
        }
    }
}
