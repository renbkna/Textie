using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Configuration;
using Textie.Core.Spammer;
using Textie.Core.Templates;

namespace Textie.Core.Cli
{
    public class DryRunCommand : AsyncCommand<DryRunCommand.Settings>
    {
        private readonly ConfigurationManager _configurationManager;
        private readonly ITemplateRenderer _templateRenderer;

        public DryRunCommand(ConfigurationManager configurationManager, ITemplateRenderer templateRenderer)
        {
            _configurationManager = configurationManager;
            _templateRenderer = templateRenderer;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await _configurationManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            var configuration = _configurationManager.CurrentConfiguration;
            var table = new Table()
                .AddColumn("#")
                .AddColumn("Message preview")
                .Border(TableBorder.Rounded);

            var random = new System.Random();
            foreach (var index in Enumerable.Range(1, settings.SampleCount))
            {
                var message = configuration.EnableTemplating
                    ? _templateRenderer.Render(configuration.Message, new SpamTemplateContext(index, configuration.Count, random))
                    : configuration.Message;
                table.AddRow(index.ToString(), message.Length > 80 ? message[..77] + "..." : message);
            }

            AnsiConsole.MarkupLine("[grey62]Dry run preview – no input will be sent.[/]");
            AnsiConsole.Write(table);
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("--samples <COUNT>")]
            public int SampleCount { get; init; } = 5;
        }
    }
}
