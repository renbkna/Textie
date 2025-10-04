using System;
using System.Threading;
using System.Threading.Tasks;
using Textie.Core.Abstractions;
using WindowsInput;
using WindowsInput.Native;

namespace Textie.Core.Infrastructure
{
    public class InputSimulatorAutomationService : ITextAutomationService, IDisposable
    {
        private readonly InputSimulator _simulator = new();
        private bool _disposed;

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InputSimulatorAutomationService));
            _simulator.Keyboard.TextEntry(text);
            return Task.CompletedTask;
        }

        public async Task TypeTextAsync(string text, int perCharacterDelayMilliseconds, CancellationToken cancellationToken)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InputSimulatorAutomationService));

            foreach (var ch in text)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _simulator.Keyboard.TextEntry(ch);
                if (perCharacterDelayMilliseconds > 0)
                {
                    await Task.Delay(perCharacterDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public Task PressEnterAsync(CancellationToken cancellationToken)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InputSimulatorAutomationService));
            _simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
