using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Configuration;

namespace Textie.Core.Cli
{
    public class ProfileListCommand : AsyncCommand
    {
        private readonly ConfigurationManager _configurationManager;

        public ProfileListCommand(ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            await _configurationManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            var profiles = await _configurationManager.GetProfilesAsync(CancellationToken.None).ConfigureAwait(false);
            if (profiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey62]No profiles saved.[/]");
                return 0;
            }

            var table = new Table()
                .AddColumn("Profile")
                .AddColumn("Last used")
                .Border(TableBorder.Rounded);

            foreach (var profile in profiles.OrderByDescending(p => p.LastUsed))
            {
                table.AddRow(profile.Name, profile.LastUsed.ToLocalTime().ToString("g"));
            }

            AnsiConsole.Write(table);
            return 0;
        }
    }
}
