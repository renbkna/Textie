using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Configuration;

namespace Textie.Core.Cli
{
    public class ProfileSaveCommand : AsyncCommand<ProfileSaveCommand.Settings>
    {
        private readonly ConfigurationManager _configurationManager;

        public ProfileSaveCommand(ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await _configurationManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            var configuration = _configurationManager.CurrentConfiguration;
            if (!configuration.IsValid())
            {
                AnsiConsole.MarkupLine("[red]Current configuration invalid. Run interactive mode first.[/]");
                return -1;
            }

            var profile = new SpamProfile
            {
                Name = settings.Name ?? "CLI profile",
                Notes = settings.Notes,
                Configuration = configuration
            };

            await _configurationManager.SaveProfileAsync(profile, CancellationToken.None).ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[green]Profile '{profile.Name}' saved.[/]");
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("--name <NAME>")]
            public string? Name { get; init; }

            [CommandOption("--notes <TEXT>")]
            public string? Notes { get; init; }
        }
    }
}
