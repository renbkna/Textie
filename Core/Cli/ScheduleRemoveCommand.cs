using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Scheduling;

namespace Textie.Core.Cli;
    public class ScheduleRemoveCommand : AsyncCommand<ScheduleRemoveCommand.Settings>
    {
        private readonly ScheduleManager _scheduleManager;

        public ScheduleRemoveCommand(ScheduleManager scheduleManager)
        {
            _scheduleManager = scheduleManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[red]Schedule name required.[/]");
                return -1;
            }

            await _scheduleManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            await _scheduleManager.RemoveAsync(settings.Name, CancellationToken.None).ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[yellow]Schedule '{Markup.Escape(settings.Name)}' removed (if it existed).[/]");
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("--name <NAME>")]
            public string Name { get; init; } = string.Empty;
        }
    }
