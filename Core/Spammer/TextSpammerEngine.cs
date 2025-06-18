using System;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using WindowsInput;
using WindowsInput.Native;
using Textie.Core.Configuration;

namespace Textie.Core.Spammer
{
    public class TextSpammerEngine : IDisposable
    {
        // Events
        public event Action? SpamStarted;
        public event Action? SpamCompleted;
        public event Action? SpamCancelled;

        // State management
        private readonly InputSimulator _inputSimulator;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _spamTask;
        private bool _disposed = false;

        public bool IsSpamming { get; private set; }

        public TextSpammerEngine()
        {
            _inputSimulator = new InputSimulator();
        }

        public async Task StartSpammingAsync(SpamConfiguration configuration)
        {
            if (IsSpamming)
                return;

            if (!configuration.IsValid())
                throw new ArgumentException("Invalid configuration", nameof(configuration));

            IsSpamming = true;
            SpamStarted?.Invoke();

            _cancellationTokenSource = new CancellationTokenSource();
            _spamTask = ExecuteSpamAsync(configuration, _cancellationTokenSource.Token);

            try
            {
                await _spamTask;
                SpamCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                SpamCancelled?.Invoke();
            }
            finally
            {
                IsSpamming = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void StopSpamming()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task ExecuteSpamAsync(SpamConfiguration config, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                AnsiConsole.Progress()
                    .Start(ctx =>
                    {
                        var progressTask = ctx.AddTask(
                            "[bold blue]Processing messages...[/]",
                            new ProgressTaskSettings
                            {
                                MaxValue = config.Count,
                                AutoStart = true
                            });

                        for (int i = 0; i < config.Count; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                SendMessage(config.Message);
                                progressTask.Increment(1);

                                // Update progress with current status
                                progressTask.Description = $"[bold blue]Processed {i + 1}/{config.Count} messages[/]";
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error processing message {i + 1}: {ex.Message}[/]");
                            }

                            // Apply delay if specified and not the last message
                            if (config.DelayMilliseconds > 0 && i < config.Count - 1)
                            {
                                Thread.Sleep(config.DelayMilliseconds);
                            }

                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        // Ensure progress bar shows completion
                        progressTask.Value = progressTask.MaxValue;
                        progressTask.Description = "[bold green]All messages processed successfully[/]";
                    });
            }, cancellationToken);
        }

        private void SendMessage(string message)
        {
            try
            {
                _inputSimulator.Keyboard.TextEntry(message);
                _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send message: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopSpamming();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }
    }
}
