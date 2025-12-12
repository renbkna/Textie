using System.Threading;
using System.Threading.Tasks;

namespace Textie.Core.Abstractions;

public interface ITextAutomationService
{
    Task SendTextAsync(string text, CancellationToken cancellationToken);
    Task TypeTextAsync(string text, int perCharacterDelayMilliseconds, CancellationToken cancellationToken);
    Task PressEnterAsync(CancellationToken cancellationToken);
}
