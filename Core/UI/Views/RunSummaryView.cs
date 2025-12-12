using System;
using Spectre.Console;
using Textie.Core.Spammer;

namespace Textie.Core.UI.Views;
    public class RunSummaryView
    {
        private readonly UiTheme _theme;

        public RunSummaryView(UiTheme theme)
        {
            _theme = theme;
        }

        public void Show(SpamRunSummary summary)
        {
            var table = new Table()
                .AddColumn(new TableColumn("Metric").Centered())
                .AddColumn(new TableColumn("Value").Centered())
                .Border(TableBorder.Rounded)
                .BorderColor(_theme.BrandAccent)
                .Expand(); // Full width looks better in the new shell

            table.AddRow(new Text("Messages Sent", _theme.MutedText), new Text(summary.MessagesSent.ToString(), _theme.Highlight));

            var duration = summary.Duration == TimeSpan.Zero ? "Instant" : summary.Duration.ToString("mm':'ss");
            table.AddRow(new Text("Duration", _theme.MutedText), new Text(duration, _theme.NormalText));

            var statusText = summary.FocusLost
                ? new Text("Stopped (Focus Lost)", _theme.Danger)
                : summary.Cancelled ? new Text("Cancelled", _theme.Warning) : new Text("Completed", _theme.Success);
            table.AddRow(new Text("Status", _theme.MutedText), statusText);

            var errorText = summary.Errors == 0
                ? new Text("0", _theme.Success)
                : new Text(summary.Errors.ToString(), _theme.Danger);
            table.AddRow(new Text("Errors", _theme.MutedText), errorText);

            var panel = new Panel(table)
            {
                Header = new PanelHeader($"[{_theme.BrandSecondary.ToMarkup()} bold]Mission Report[/]"),
                Border = BoxBorder.Rounded
            }
            .BorderColor(_theme.BrandPrimary)
            .Padding(1, 1);

            AnsiConsole.Write(panel);
        }
    }
