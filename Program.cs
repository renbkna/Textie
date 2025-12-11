using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Textie.Core;
using Textie.Core.Abstractions;
using Textie.Core.Cli;
using Textie.Core.Configuration;
using Textie.Core.Infrastructure;
using Textie.Core.Input;
using Textie.Core.Scheduling;
using Textie.Core.Spammer;
using Textie.Core.Templates;
using Textie.Core.UI;

namespace Textie
{
    public static class Program
    {
        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.AddConsole();

            builder.Services.AddSingleton<IConfigurationStore, ConfigurationStore>();
            builder.Services.AddSingleton<ConfigurationManager>();
            builder.Services.AddSingleton<IUserInterface, UserInterface>();
            builder.Services.AddSingleton<IHotkeyService, GlobalKeyboardHook>();
            builder.Services.AddSingleton<ITextAutomationService, NativeInputService>();
            builder.Services.AddSingleton<ITemplateRenderer, FastTemplateRenderer>();
            builder.Services.AddSingleton<TextSpammerEngine>();
            builder.Services.AddSingleton<ScheduleManager>();
            builder.Services.AddSingleton<TextieApplication>();

            builder.Services.AddTransient<RunCommand>();
            builder.Services.AddTransient<DryRunCommand>();
            builder.Services.AddTransient<ProfileListCommand>();
            builder.Services.AddTransient<ProfileSaveCommand>();
            builder.Services.AddTransient<ProfileDeleteCommand>();
            builder.Services.AddTransient<ScheduleListCommand>();
            builder.Services.AddTransient<ScheduleAddCommand>();
            builder.Services.AddTransient<ScheduleRemoveCommand>();
            builder.Services.AddTransient<ScheduleRunCommand>();

            using var host = builder.Build();

            var registrar = new SpectreTypeRegistrar(host.Services);
#pragma warning disable IL3050
            var commandApp = new CommandApp(registrar);
#pragma warning restore IL3050
            commandApp.Configure(config =>
            {
                config.SetApplicationName("textie");
                config.PropagateExceptions();
                config.AddCommand<RunCommand>("run").WithDescription("Run automation headlessly.");
                config.AddCommand<DryRunCommand>("dry-run").WithDescription("Preview templated messages without sending.");
                config.AddCommand<BenchmarkCommand>("benchmark").WithDescription("Verify system performance and optimizations.");
                config.AddBranch("profile", branch =>
                {
                    branch.AddCommand<ProfileListCommand>("list").WithDescription("List saved profiles.");
                    branch.AddCommand<ProfileSaveCommand>("save").WithDescription("Save the current configuration as a profile.");
                    branch.AddCommand<ProfileDeleteCommand>("delete").WithDescription("Delete a saved profile.");
                });
                config.AddBranch("schedule", branch =>
                {
                    branch.AddCommand<ScheduleListCommand>("list").WithDescription("List scheduled automations.");
                    branch.AddCommand<ScheduleAddCommand>("add").WithDescription("Add or update a schedule.");
                    branch.AddCommand<ScheduleRemoveCommand>("remove").WithDescription("Remove a schedule.");
                    branch.AddCommand<ScheduleRunCommand>("run").WithDescription("Execute all due schedules now.");
                });
            });

            if (args.Length > 0)
            {
                return await commandApp.RunAsync(args).ConfigureAwait(false);
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Task.Run(static () => Environment.Exit(0));
            };

            var app = host.Services.GetRequiredService<TextieApplication>();
            await app.RunAsync(cts.Token).ConfigureAwait(false);
            return 0;
        }
    }
}
