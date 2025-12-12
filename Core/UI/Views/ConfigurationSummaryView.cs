using System;
using Spectre.Console;
using Textie.Core.Configuration;
using Textie.Core.Spammer;

namespace Textie.Core.UI.Views;
    public class ConfigurationSummaryView
    {
        private readonly UiTheme _theme;

        public ConfigurationSummaryView(UiTheme theme)
        {
            _theme = theme;
        }

        public Panel GetPanel(SpamConfiguration config)
        {
            var grid = new Grid();
            grid.AddColumn(new GridColumn().PadRight(2));
            grid.AddColumn(new GridColumn().PadRight(2));
            grid.AddColumn(new GridColumn());

            grid.AddRow(
                new Text("Parameter", _theme.MutedText),
                new Text("Value", _theme.NormalText),
                new Text("Assessment", _theme.MutedText));

            grid.AddRow(
                new Text("Message", _theme.Highlight),
                GetMessagePreview(config.Message),
                GetMessageAssessment(config.Message.Length));

            grid.AddRow(
                new Text("Count", _theme.Highlight),
                new Text($"{config.Count:N0}", _theme.NormalText),
                GetCountAssessment(config.Count));

            grid.AddRow(
                new Text("Delay", _theme.Highlight),
                new Text($"{config.DelayMilliseconds} ms", _theme.NormalText),
                GetDelayAssessment(config.DelayMilliseconds));

            grid.AddRow(
                new Text("Strategy", _theme.Highlight),
                new Text(config.Strategy.ToString(), _theme.NormalText),
                GetStrategyAssessment(config.Strategy));

            return new Panel(grid)
            {
                Header = new PanelHeader($"[{_theme.BrandSecondary.ToMarkup()} bold]Configuration Summary[/]")
            }
            .BorderColor(_theme.BrandAccent)
            .Padding(1, 1);
        }

        private Text GetMessagePreview(string message)
        {
            var safe = message.Length > 25 ? message[..22] + "..." : message;
            return new Text(safe, _theme.NormalText); // Escaping handled by Text class hopefully, or we use Markup.Escape
        }

        private Text GetMessageAssessment(int length) => length switch
        {
            <= 50 => new Text("Optimal", _theme.Success),
            <= 200 => new Text("Long", _theme.Warning),
            _ => new Text("Very Long", _theme.Danger)
        };

        private Text GetCountAssessment(int count) => count switch
        {
            <= 10 => new Text("Small Batch", _theme.Success),
            <= 1000 => new Text("High Volume", _theme.Warning),
            _ => new Text("Extreme", _theme.Danger)
        };

        private Text GetDelayAssessment(int delay) => delay switch
        {
            0 => new Text("Unsafe Speed", _theme.Danger),
            <= 50 => new Text("Aggressive", _theme.Warning),
            _ => new Text("Stable", _theme.Success)
        };

        private Text GetStrategyAssessment(SpamStrategy strategy) => strategy switch
        {
            SpamStrategy.TypePerCharacter => new Text("Human-like", _theme.Success),
            SpamStrategy.SendTextOnly => new Text("Raw Input", _theme.Warning),
            _ => new Text("Standard", _theme.NormalText)
        };
    }
