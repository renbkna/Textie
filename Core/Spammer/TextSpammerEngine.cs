using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Textie.Core.Abstractions;
using Textie.Core.Configuration;
using Textie.Core.Templates;

namespace Textie.Core.Spammer
{
    public sealed class TextSpammerEngine : IDisposable
    {
        private readonly ITextAutomationService _automationService;
        private readonly ITemplateRenderer _templateRenderer;
        private readonly ILogger<TextSpammerEngine> _logger;
        private readonly SemaphoreSlim _executionLock = new(1, 1);

        private CancellationTokenSource? _runCancellationSource;
        private bool _disposed;

        public TextSpammerEngine(ITextAutomationService automationService, ITemplateRenderer templateRenderer, ILogger<TextSpammerEngine> logger)
        {
            _automationService = automationService;
            _templateRenderer = templateRenderer;
            _logger = logger;
        }

        public bool IsSpamming { get; private set; }

        public event EventHandler? SpamStarted;
        public event EventHandler<SpamProgressEventArgs>? ProgressChanged;
        public event EventHandler<SpamRunSummary>? SpamCompleted;
        public event EventHandler<SpamRunSummary>? SpamCancelled;
        public event EventHandler<Exception>? SpamFailed;

        public async Task<SpamRunSummary?> StartSpammingAsync(SpamConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (!configuration.IsValid()) throw new ArgumentException("Invalid configuration", nameof(configuration));
            if (_disposed) throw new ObjectDisposedException(nameof(TextSpammerEngine));

            await _executionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (IsSpamming)
                {
                    _logger.LogWarning("Spam request ignored because an operation is already running.");
                    return null;
                }

                IsSpamming = true;
                _runCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var linkedToken = _runCancellationSource.Token;

                SpamStarted?.Invoke(this, EventArgs.Empty);

                try
                {
                    var summary = await ExecuteSpamAsync(configuration, linkedToken).ConfigureAwait(false);
                    SpamCompleted?.Invoke(this, summary);
                    return summary;
                }
                catch (OperationCanceledException)
                {
                    var summary = new SpamRunSummary { Cancelled = true };
                    SpamCancelled?.Invoke(this, summary);
                    return summary;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Spam execution failed.");
                    SpamFailed?.Invoke(this, ex);
                    throw;
                }
                finally
                {
                    IsSpamming = false;
                    _runCancellationSource?.Dispose();
                    _runCancellationSource = null;
                }
            }
            finally
            {
                _executionLock.Release();
            }
        }

        public void StopSpamming()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TextSpammerEngine));
            _runCancellationSource?.Cancel();
        }

        private async Task<SpamRunSummary> ExecuteSpamAsync(SpamConfiguration config, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var summary = new SpamRunSummary();
            var random = new Random();

            for (int index = 0; index < config.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new SpamTemplateContext(index + 1, config.Count, random);
                var message = config.EnableTemplating
                    ? _templateRenderer.Render(config.Message, context)
                    : config.Message;

                try
                {
                    await ExecuteStrategyAsync(config, message, cancellationToken).ConfigureAwait(false);
                    summary.MessagesSent++;
                    ProgressChanged?.Invoke(this, new SpamProgressEventArgs(summary.MessagesSent, config.Count, $"Sent {summary.MessagesSent}/{config.Count}"));
                }
                catch (Exception ex)
                {
                    summary.Errors++;
                    _logger.LogError(ex, "Failed to send message {Index}.", index + 1);
                    SpamFailed?.Invoke(this, ex);
                }

                if (index < config.Count - 1)
                {
                    var delay = CalculateDelay(config, random);
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            stopwatch.Stop();
            summary.Duration = stopwatch.Elapsed;
            return summary;
        }

        private async Task ExecuteStrategyAsync(SpamConfiguration config, string message, CancellationToken cancellationToken)
        {
            switch (config.Strategy)
            {
                case SpamStrategy.SendTextAndEnter:
                    await _automationService.SendTextAsync(message, cancellationToken).ConfigureAwait(false);
                    if (config.SendSubmitKey)
                    {
                        await _automationService.PressEnterAsync(cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case SpamStrategy.SendTextOnly:
                    await _automationService.SendTextAsync(message, cancellationToken).ConfigureAwait(false);
                    if (config.SendSubmitKey)
                    {
                        await _automationService.PressEnterAsync(cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case SpamStrategy.TypePerCharacter:
                    await _automationService.TypeTextAsync(message, config.PerCharacterDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                    if (config.SendSubmitKey)
                    {
                        await _automationService.PressEnterAsync(cancellationToken).ConfigureAwait(false);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(config.Strategy), config.Strategy, "Unsupported spam strategy");
            }
        }

        private static TimeSpan CalculateDelay(SpamConfiguration config, Random random)
        {
            var baseDelay = config.DelayMilliseconds;
            if (baseDelay <= 0)
            {
                return TimeSpan.Zero;
            }

            if (config.DelayJitterPercent <= 0)
            {
                return TimeSpan.FromMilliseconds(baseDelay);
            }

            var jitterAmplitude = baseDelay * config.DelayJitterPercent / 100.0;
            var offset = (random.NextDouble() * 2 - 1) * jitterAmplitude;
            var jittered = Math.Max(0, baseDelay + offset);
            return TimeSpan.FromMilliseconds(jittered);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _executionLock.Dispose();
            _runCancellationSource?.Dispose();
            (_automationService as IDisposable)?.Dispose();
            _disposed = true;
        }
    }
}
