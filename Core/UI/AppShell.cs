using System;
using Spectre.Console;

namespace Textie.Core.UI;
    public enum RenderMode
    {
        /// <summary>Clears screen before each render - for fresh views.</summary>
        Full,
        /// <summary>Preserves console state - for interactive prompts.</summary>
        Interactive
    }

    public class AppShell
    {
        private readonly UiTheme _theme;
        private bool _headerRendered;

        public AppShell(UiTheme theme)
        {
            _theme = theme;
        }

        public void Render(Action contentRenderer) => Render(contentRenderer, RenderMode.Full);

        public void Render(Action contentRenderer, RenderMode mode)
        {
            if (mode == RenderMode.Full)
            {
                AnsiConsole.Clear();
                _headerRendered = false;
            }

            if (!_headerRendered)
            {
                RenderHeader();
                _headerRendered = true;
            }

            try
            {
                contentRenderer();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Render Error: {Markup.Escape(ex.Message)}[/]");
            }
            finally
            {
                if (mode == RenderMode.Full)
                {
                    RenderFooter();
                }
            }
        }

        public void ResetHeader() => _headerRendered = false;

        private void RenderHeader()
        {
            var title = new FigletText("TEXTIE")
                .Color(_theme.BrandSecondary)
                .Centered();

            AnsiConsole.Write(title);

            var rule = new Rule("[cyan1]CYBERNETIC TEXT AUTOMATION[/]")
            {
                Style = _theme.PanelBorder
            };
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        private void RenderFooter()
        {
            AnsiConsole.WriteLine();
            var rule = new Rule($"[grey50]v{UiTheme.AppVersion} :: SYSTEM READY[/]")
            {
                Style = _theme.PanelBorder
            };
            AnsiConsole.Write(rule);
        }
    }
