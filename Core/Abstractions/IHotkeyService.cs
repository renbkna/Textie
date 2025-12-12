using System;
using System.Threading;
using System.Threading.Tasks;

namespace Textie.Core.Abstractions;

public enum HotkeyAction
{
    None,
    Start,
    Stop,
    Exit
}

public interface IHotkeyService : IDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<HotkeyAction> WaitForNextAsync(CancellationToken cancellationToken);
    void SignalCompletion();
}
