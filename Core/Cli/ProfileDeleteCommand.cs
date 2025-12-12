using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Configuration;

namespace Textie.Core.Cli;
    public class ProfileDeleteCommand : AsyncCommand<ProfileDeleteCommand.Settings>
    {
        private readonly ConfigurationManager _configurationManager;

        public ProfileDeleteCommand(ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[red]Profile name required.[/]");
                return -1;
            }

            await _configurationManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            await _configurationManager.DeleteProfileAsync(settings.Name, CancellationToken.None).ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[yellow]Profile '{Markup.Escape(settings.Name)}' deleted (if it existed).[/]");
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("--name <NAME>")]
            public string Name { get; init; } = string.Empty;
        }
    }
