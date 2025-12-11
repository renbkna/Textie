using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Textie.Core.Configuration;
using Textie.Core.Infrastructure;
using Textie.Core.Spammer;
using Textie.Core.Templates;
using Microsoft.Extensions.Logging.Abstractions;

namespace Textie.Core.Cli
{
    public class BenchmarkCommand : AsyncCommand
    {
        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            AnsiConsole.Write(new FigletText("BENCHMARK").Color(Color.Cyan1));
            AnsiConsole.MarkupLine("[bold yellow]Running Ludicrous Speed Diagnostics...[/]");
            AnsiConsole.WriteLine();

            // 1. Thread Affinity Check
            AnsiConsole.MarkupLine("[bold]1. Thread Affinity Check[/]");
            var process = Process.GetCurrentProcess();
            var affinity = process.ProcessorAffinity.ToInt64();
            AnsiConsole.MarkupLine($"   Process Affinity Mask: [cyan]0x{affinity:X}[/]");
            AnsiConsole.MarkupLine($"   Core Count: [cyan]{Environment.ProcessorCount}[/]");

            // We can't easily verify the *Engine's* thread affinity from here since it happens inside StartSpammingAsync,
            // but we can verify that we *can* set it.
            try
            {
                long mask = 1L << (Environment.ProcessorCount - 1);
                if ((affinity & mask) != 0)
                {
                    AnsiConsole.MarkupLine("   [green]PASS:[/] Application has access to the target high-performance core.");
                }
                else
                {
                    AnsiConsole.MarkupLine("   [red]FAIL:[/] Process generates invalid affinity mask for hardware.");
                }
            }
            catch (Exception ex)
            {
                 AnsiConsole.MarkupLine($"   [red]FAIL:[/] Affinity Check Error: {ex.Message}");
            }
            AnsiConsole.WriteLine();

            // 2. Timing Precision (SpinWait Verification)
            AnsiConsole.MarkupLine("[bold]2. Timing Precision (1ms Target)[/]");

            double totalError = 0;
            int iterations = 50;
            long freq = Stopwatch.Frequency;

            await AnsiConsole.Status().StartAsync("Measuring Inter-Message Delay...", async ctx =>
            {
                // Warmup
                Thread.SpinWait(100);

                for (int i = 0; i < iterations; i++)
                {
                    long start = Stopwatch.GetTimestamp();

                    // Simulate the logic inside TextSpammerEngine for < 20ms delays
                    // Delay 1ms
                    double delayMs = 1.0;
                    var limit = start + (long)(delayMs * TimeSpan.TicksPerMillisecond * (freq / (double)TimeSpan.TicksPerSecond));

                    while (Stopwatch.GetTimestamp() < limit)
                    {
                        Thread.SpinWait(10);
                    }

                    long end = Stopwatch.GetTimestamp();
                    double elapsedMs = (double)(end - start) * 1000 / freq;
                    totalError += Math.Abs(elapsedMs - 1.0);
                }
            });

            double avgError = totalError / iterations;
            AnsiConsole.MarkupLine($"   Average Deviation: [cyan]{avgError:F4} ms[/]");
            if (avgError < 0.1) // 100 microseconds
            {
                AnsiConsole.MarkupLine("   [green]PASS:[/] SpinWait is providing sub-millisecond precision.");
            }
            else if (avgError < 15.0)
            {
                AnsiConsole.MarkupLine("   [yellow]WARN:[/] Deviation > 0.1ms but < 15ms. Better than standard sleep.");
            }
            else
            {
                AnsiConsole.MarkupLine("   [red]FAIL:[/] Deviation matches standard OS Timer resolution (~15ms). Optimization failed.");
            }
            AnsiConsole.WriteLine();

            // 3. Engine Throughput
            AnsiConsole.MarkupLine("[bold]3. Engine Loop Throughput (Zero-Delay)[/]");

            // create distinct engine for bench
            var nullInput = new NullAutomationService();
            var renderer = new FastTemplateRenderer();
            var engine = new TextSpammerEngine(nullInput, renderer, NullLogger<TextSpammerEngine>.Instance);

            var config = new SpamConfiguration
            {
                Message = "BENCH",
                Count = 10000,
                DelayMilliseconds = 0,
                DelayJitterPercent = 0,
                Strategy = SpamStrategy.SendTextOnly,
                SendSubmitKey = false,
                EnableTemplating = false
            };

            long runStart = Stopwatch.GetTimestamp();
            await engine.StartSpammingAsync(config, CancellationToken.None);
            long runEnd = Stopwatch.GetTimestamp();

            double runSeconds = (double)(runEnd - runStart) / freq;
            double msgPerSec = config.Count / runSeconds;

            AnsiConsole.MarkupLine($"   Messages Sent: [cyan]{config.Count}[/]");
            AnsiConsole.MarkupLine($"   Total Time: [cyan]{runSeconds:F4} s[/]");
            AnsiConsole.MarkupLine($"   Throughput: [green bold]{msgPerSec:N0} msgs/sec[/]");

            AnsiConsole.WriteLine();
            return 0;
        }
    }
}
