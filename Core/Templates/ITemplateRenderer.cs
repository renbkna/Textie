using Textie.Core.Spammer;

namespace Textie.Core.Templates
{
    public interface ITemplateRenderer
    {
        string Render(string template, SpamTemplateContext context);
    }
}
