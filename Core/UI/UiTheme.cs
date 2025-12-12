using System.Reflection;
using Spectre.Console;

namespace Textie.Core.UI;
    public class UiTheme
    {
        // App Metadata
        public static string AppVersion { get; } = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.0.0";

        // Core Brand Colors - "Cyberpunk/Neon" Logic
        public Color BrandPrimary { get; } = Color.Cyan1;
        public Color BrandSecondary { get; } = Color.DeepPink1;
        public Color BrandAccent { get; } = Color.Purple3;

        // Semantic Colors
        public Color Success { get; } = Color.SpringGreen3;
        public Color Warning { get; } = Color.Gold1;
        public Color Danger { get; } = Color.Red1;
        public Color Neutral { get; } = Color.Grey46;
        public Color Background { get; } = Color.Black;

        // Wizard Step Indicator Colors
        public Color StepComplete { get; } = Color.SpringGreen3;
        public Color StepCurrent { get; } = Color.Cyan1;
        public Color StepPending { get; } = Color.Grey50;

        // Rich Styles
        public Style PrimaryHeader { get; }
        public Style SecondaryHeader { get; }
        public Style NormalText { get; }
        public Style MutedText { get; }
        public Style Highlight { get; }
        public Style PanelBorder { get; }
        public Style PromptTitle { get; }

        public UiTheme()
        {
            PrimaryHeader = new Style(BrandPrimary, decoration: Decoration.Bold);
            SecondaryHeader = new Style(BrandSecondary, decoration: Decoration.Bold);
            NormalText = new Style(Color.White);
            MutedText = new Style(Color.Grey58);
            Highlight = new Style(BrandPrimary, decoration: Decoration.Bold);
            PanelBorder = new Style(BrandAccent);
            PromptTitle = new Style(BrandSecondary, decoration: Decoration.Bold);
        }
    }
