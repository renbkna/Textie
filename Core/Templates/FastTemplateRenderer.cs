using System;
using System.Text;
using Textie.Core.Spammer;

namespace Textie.Core.Templates;
    public class FastTemplateRenderer : ITemplateRenderer
    {
        // Simple, fast tokenizer supporting {index}, {total}, {timestamp}, {guid}, {random}
        // No reflection, minimized allocations.

        public string Render(string template, SpamTemplateContext context)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            // Estimate capacity to avoid re-allocations
            var sb = new StringBuilder(template.Length + 64);
            var span = template.AsSpan();

            int i = 0;
            while (i < span.Length)
            {
                int openBrace = template.IndexOf('{', i);

                if (openBrace == -1)
                {
                    // No more placeholders, append rest
                    sb.Append(template, i, span.Length - i);
                    break;
                }

                // Append text before brace
                if (openBrace > i)
                {
                    sb.Append(template, i, openBrace - i);
                }

                int closeBrace = template.IndexOf('}', openBrace);
                if (closeBrace == -1)
                {
                    // Malformed, append rest
                    sb.Append(template, openBrace, span.Length - openBrace);
                    break;
                }

                // Extract token
                var token = span.Slice(openBrace + 1, closeBrace - openBrace - 1);

                ProcessToken(token, sb, context);

                i = closeBrace + 1;
            }

            return sb.ToString();
        }

        private void ProcessToken(ReadOnlySpan<char> token, StringBuilder sb, SpamTemplateContext context)
        {
            if (token.Equals("index", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(context.Index);
            }
            else if (token.Equals("total", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(context.Total);
            }
            else if (token.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(context.Timestamp.ToString("HH:mm:ss"));
            }
            else if (token.Equals("guid", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(context.MessageGuid);
            }
            else if (token.Equals("random", StringComparison.OrdinalIgnoreCase) || token.Equals("rand", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(context.Random.Next());
            }
            else
            {
                // Unknown token, keep as is
                sb.Append('{');
                sb.Append(token);
                sb.Append('}');
            }
        }
    }
