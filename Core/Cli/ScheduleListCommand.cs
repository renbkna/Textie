using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Scheduling;

namespace Textie.Core.Cli
{
    public class ScheduleListCommand : AsyncCommand
    {
        private readonly ScheduleManager _scheduleManager;

        public ScheduleListCommand(ScheduleManager scheduleManager)
        {
            _scheduleManager = scheduleManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            await _scheduleManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            var schedules = await _scheduleManager.GetSchedulesAsync(CancellationToken.None).ConfigureAwait(false);
            if (schedules.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey62]No schedules configured.[/]");
                return 0;
            }

            var table = new Table()
                .AddColumn("Schedule")
                .AddColumn("Profile")
                .AddColumn("Cron")
                .AddColumn("Enabled")
                .AddColumn("Next run")
                .Border(TableBorder.Rounded);

            foreach (var schedule in schedules.OrderBy(s => s.Name))
            {
                table.AddRow(
                    schedule.Name,
                    schedule.ProfileName,
                    schedule.CronExpression,
                    schedule.Enabled ? "[green]Yes[/]" : "[red]No[/]",
                    schedule.NextRun?.ToString("g") ?? "n/a");
            }

            AnsiConsole.Write(table);
            return 0;
        }
    }
}
