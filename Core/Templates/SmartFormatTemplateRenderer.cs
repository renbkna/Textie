using System;
using SmartFormat;
using Textie.Core.Spammer;

namespace Textie.Core.Templates
{
    public class SmartFormatTemplateRenderer : ITemplateRenderer
    {
        private readonly SmartFormatter _formatter;

        public SmartFormatTemplateRenderer()
        {
            _formatter = Smart.CreateDefaultSmartFormat();
        }

        public string Render(string template, SpamTemplateContext context)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            try
            {
                return _formatter.Format(template, new
                {
                    index = context.Index,
                    total = context.Total,
                    timestamp = context.Timestamp,
                    guid = context.MessageGuid,
                    random = context.Random,
                    rand = context.Random.Next()
                });
            }
            catch (Exception)
            {
                return template;
            }
        }
    }
}
