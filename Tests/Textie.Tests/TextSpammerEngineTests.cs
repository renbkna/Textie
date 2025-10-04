using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Textie.Core.Abstractions;
using Textie.Core.Configuration;
using Textie.Core.Spammer;
using Textie.Core.Templates;
using Xunit;

namespace Textie.Tests
{
    public class TextSpammerEngineTests
    {
        [Fact]
        public async Task StartSpammingAsync_SendsAllMessages()
        {
            var automation = new FakeAutomationService();
            var renderer = new PassThroughTemplateRenderer();
            var engine = new TextSpammerEngine(automation, renderer, NullLogger<TextSpammerEngine>.Instance);
            var config = new SpamConfiguration
            {
                Message = "Hello",
                Count = 3,
                DelayMilliseconds = 0,
                Strategy = SpamStrategy.SendTextOnly,
                SendSubmitKey = false
            };

            int lastProgress = 0;
            engine.ProgressChanged += (_, args) => lastProgress = args.Current;

            var summary = await engine.StartSpammingAsync(config, CancellationToken.None);

            Assert.NotNull(summary);
            Assert.Equal(3, summary!.MessagesSent);
            Assert.Equal(3, automation.TextSendCount);
            Assert.Equal(0, automation.EnterPressCount);
            Assert.Equal(3, lastProgress);
        }

        [Fact]
        public async Task StopSpamming_CancelsRun()
        {
            var automation = new FakeAutomationService(delayMilliseconds: 20);
            var renderer = new PassThroughTemplateRenderer();
            var engine = new TextSpammerEngine(automation, renderer, NullLogger<TextSpammerEngine>.Instance);
            var config = new SpamConfiguration
            {
                Message = "Hello",
                Count = 100,
                DelayMilliseconds = 10,
                Strategy = SpamStrategy.SendTextOnly,
                SendSubmitKey = false
            };

            var runTask = engine.StartSpammingAsync(config, CancellationToken.None);
            await Task.Delay(50);
            engine.StopSpamming();

            var summary = await runTask;
            Assert.NotNull(summary);
            Assert.True(summary!.Cancelled);
        }

        private sealed class FakeAutomationService : ITextAutomationService
        {
            private readonly int _delayMilliseconds;

            public FakeAutomationService(int delayMilliseconds = 0)
            {
                _delayMilliseconds = delayMilliseconds;
            }

            public int TextSendCount { get; private set; }
            public int EnterPressCount { get; private set; }

            public Task PressEnterAsync(CancellationToken cancellationToken)
            {
                EnterPressCount++;
                return Task.CompletedTask;
            }

            public async Task SendTextAsync(string text, CancellationToken cancellationToken)
            {
                TextSendCount++;
                if (_delayMilliseconds > 0)
                {
                    await Task.Delay(_delayMilliseconds, cancellationToken);
                }
            }

            public async Task TypeTextAsync(string text, int perCharacterDelayMilliseconds, CancellationToken cancellationToken)
            {
                TextSendCount += text.Length;
                if (perCharacterDelayMilliseconds > 0)
                {
                    await Task.Delay(perCharacterDelayMilliseconds, cancellationToken);
                }
            }
        }

        private sealed class PassThroughTemplateRenderer : ITemplateRenderer
        {
            public string Render(string template, SpamTemplateContext context) => template;
        }
    }
}
