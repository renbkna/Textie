using System;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Textie.Core.Configuration;
using Textie.Core.Spammer;

namespace Textie.Core.UI.Views;
    public class LiveDashboardView
    {
        private readonly UiTheme _theme;

        public LiveDashboardView(UiTheme theme)
        {
            _theme = theme;
        }

        public void ShowWaitingState(SpamConfiguration config)
        {
            var layout = new Layout("Dashboard")
                .SplitColumns(
                    new Layout("Status").Ratio(2),
                    new Layout("Controls").Ratio(1));

            var statusPanel = new Panel(
                    Align.Center(
                        new Markup($"[bold {_theme.BrandPrimary.ToMarkup()}]SYSTEM ARMED[/]\n\n" +
                                   $"Target: [white]{(config.LockTargetWindow ? config.TargetWindowTitle : "Any Window")}[/]\n" +
                                   $"Payload: [white]{config.Count:N0} messages[/]\n" +
                                   $"Interval: [white]{config.DelayMilliseconds}ms[/]"),
                        VerticalAlignment.Middle))
                .BorderColor(_theme.BrandPrimary)
                .Header("Status");

            var controlsPanel = new Panel(
                    Align.Center(
                        new Markup("[green]ENTER[/] to ENGAGE\n[red]ESC[/] to ABORT"),
                        VerticalAlignment.Middle))
                .BorderColor(_theme.Neutral)
                .Header("Manual Override");

            layout["Status"].Update(statusPanel);
            layout["Controls"].Update(controlsPanel);

            AnsiConsole.Write(layout);
        }

        // Note: The actual "Running" state logic involves a ProgressTask which is dynamic.
        // We might keep that in the main controller or encapsulate it here if we want to get fancy with AnsiConsole.Live
        // For now, let's provide a helper for the "Running" header.

        public ProgressColumn[] GetProgressColumns()
        {
            return new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(Spinner.Known.Dots) // Cyberpunk-ish spinner
            };
        }
    }
