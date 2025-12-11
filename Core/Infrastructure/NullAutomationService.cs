using System.Threading;
using System.Threading.Tasks;
using Textie.Core.Abstractions;

namespace Textie.Core.Infrastructure
{
    public sealed class NullAutomationService : ITextAutomationService
    {
        public Task PressEnterAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task TypeTextAsync(string text, int perCharacterDelayMilliseconds, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
