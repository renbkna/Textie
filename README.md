# Textie

Windows-first text automation for power users and QA engineers. Textie delivers an advanced Spectre.Console terminal experience, global hotkeys, templated messaging, profiles, and a CLI/scheduler for unattended runs.

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/OS-Windows-blue.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Highlights

- **Premium Spectre TUI** – Figlet banner, stepper wizard, live dashboards, and run summaries.
- **Smart configuration wizard** – Profiles, templating preview, jitter, strategies, and focus lock in one guided flow.
- **Advanced automation engine** – Async pipeline, jittered delays, per-character typing, templated payloads, optional submit key.
- **Global hotkeys** – ENTER to start, ESC to cancel, backed by a signal-based low-level hook (no busy loops).
- **Profiles & persistence** – Settings and profiles stored under `%AppData%/Textie`; tolerant JSON persistence.
- **CLI commands** – Headless `run`, `dry-run`, `profile` management, and `schedule` commands using Spectre.Console.Cli.
- **Scheduling** – Cron-powered scheduled runs (via `NCrontab`) persisted alongside profiles.
- **Telemetry-friendly logging** – Microsoft.Extensions.Logging with console provider; consistent error surfacing in UI.
- **Unit tests** – Core engine behaviour covered with xUnit (requires Windows Desktop runtime locally).

## Quick Start

```bash
# clone
git clone https://github.com/yourusername/textie.git
cd textie

# restore + build (Windows)
dotnet restore
dotnet build

# run interactive UI
dotnet run
```

> **Note:** The project targets `net10.0-windows` and depends on `Microsoft.WindowsDesktop.App`. Build and runtime require Windows or a Windows-targeting SDK installation.

## Interactive Experience

1. **Wizard** – Launch presents a six-step configuration stepper (Message → Count → Delay → Strategy → Advanced → Review). Warnings are non-blocking and surfaced inline.
2. **Profiles** – Load existing profiles, reset to defaults, or save new ones during the review step.
3. **Dashboard** – After confirming, the waiting dashboard displays active configuration, profiles, and hotkey hints.
4. **Automation run** – ENTER to start. Live progress bars (Spectre progress columns), templated status, and cancellation via ESC.
5. **Summary** – Completion panel with metrics (messages, duration, errors, cancellation state) followed by next-action prompt.

## CLI Commands

Textie ships with a Spectre.Console.Cli command surface. To view help:

```bash
dotnet run -- --help
```

Key commands:

| Command | Description |
|---------|-------------|
| `run [options]` | Headless run with optional overrides (`--profile`, `--message`, `--count`, `--delay`, `--focus-delay`, `--preview`). |
| `dry-run [--samples N]` | Render templated messages without sending input. |
| `profile list` | Show saved profiles with timestamps. |
| `profile save --name <NAME>` | Persist the current configuration as a profile (and optional notes). |
| `profile delete --name <NAME>` | Remove a stored profile. |
| `schedule list` | Display scheduled runs, target profiles, cron expressions, and next occurrence. |
| `schedule add --name <NAME> --profile <PROFILE> --cron "0 8 * * *"` | Create/update a scheduled run (optionally `--disabled`). |
| `schedule remove --name <NAME>` | Delete a scheduled run. |
| `schedule run` | Execute all due schedules immediately (updates next-run timestamps). |

## Automation Strategies & Options

- **Strategies** – `SendTextAndEnter`, `SendTextOnly`, `TypePerCharacter`.
- **Templating** – Built-in fast template renderer with placeholders (`{index}`, `{total}`, `{timestamp}`, `{guid}`, `{random}`, `{rand}`).
- **Timing** – Base delay (0–120,000ms) with optional jitter (0–100%) and per-character delays for humanized typing.
- **Submit Key** – Toggle Enter submission regardless of strategy.
- **Focus control** – Optional target window title + focus lock reminder before and during runs.

## Configuration & Persistence

- Configuration file: `%AppData%/Textie/settings.json`
- Profiles file: `%AppData%/Textie/profiles.json`
- Schedules file: `%AppData%/Textie/schedules.json`

Files are written with indentation, ignore defaults on failure, and are resilient to missing/corrupt data.

## Architecture Overview

```
├── Program.cs                         # Host builder, DI, CLI wiring
├── Core/
│   ├── Abstractions/                 # Interfaces (hotkeys, template, config)
│   ├── Configuration/                # Configuration models + manager
│   ├── Input/                        # Global keyboard hook implementing IHotkeyService
│   ├── Infrastructure/               # Config store, NativeInputService (P/Invoke), Spectre registrar
│   ├── Scheduling/                   # Schedule manager and models (NCrontab)
│   ├── Spammer/                      # Engine, progress events, summaries, templating context
│   ├── Templates/                    # FastTemplateRenderer (zero-alloc, span-based)
│   └── UI/                           # IUserInterface + Spectre implementation (wizard, dashboard, run view)
├── Core/Cli/                         # CLI command handlers (run, dry-run, profile, schedule)
└── Tests/Textie.Tests/               # xUnit project covering engine behaviour
```

### Dependency Graph

- **TextieApplication** orchestrates configuration manager, UI, hotkeys, engine, and logging.
- **ConfigurationManager** & **ConfigurationStore** persist settings/profiles/schedules via async JSON.
- **TextSpammerEngine** consumes `ITextAutomationService` (native P/Invoke via `NativeInputService`) and `ITemplateRenderer` to drive automation with cancellation and jitter.
- **UserInterface** (Spectre) renders wizard, dashboards, and progress; hosts CLI progress output for headless runs via shared engine events.
- **Hotkey Service** uses Win32 low-level hook with TaskCompletionSource signalling (no polling loops).
- **CLI** commands reuse DI-registered services for headless operations and scheduling maintenance.

## Testing

```bash
# Build everything
dotnet build textie.sln

# Run tests (requires Microsoft.WindowsDesktop runtime on Windows)
dotnet test Tests/Textie.Tests/Textie.Tests.csproj
```

> In non-Windows environments the WindowsDesktop runtime is unavailable; tests will fail to launch the xUnit testhost. Run tests on Windows or install the Windows Desktop SDK.

## Dependencies

- [Spectre.Console 0.51.1](https://spectreconsole.net/) (core TUI)
- [Spectre.Console.Cli 0.51.1](https://spectreconsole.net/) (CLI surface)
- [Spectre.Console.ImageSharp 0.51.1](https://spectreconsole.net/) (optional branding)
- [Microsoft.Extensions.Configuration.Json 9.0.9]
- [Microsoft.Extensions.DependencyInjection 9.0.9]
- [Microsoft.Extensions.Hosting 9.0.9]
- [Microsoft.Extensions.Logging.Console 9.0.9]
- [NCrontab 3.4.0]
- [Vanara.PInvoke.User32 4.2.1] (Win32 API wrappers)
- [xUnit 2.9.2] (tests)

## Troubleshooting

| Symptom | Resolution |
|---------|------------|
| Keyboard hotkeys ignored | Run as Administrator, ensure no security software blocks low-level hooks. |
| No input delivered | Confirm target window focus; increase delays; disable focus lock if target changes title during run. |
| Scheduler not executing | `schedule list` to confirm cron and next run; ensure schedule is enabled; run CLI with `dotnet run -- schedule list`. |
| CLI fails on Linux/macOS | CLI is Windows-focused; ensure Windows Desktop runtime present. |
| Tests fail to run | Install .NET 10 Windows Desktop runtime or run tests on Windows environment. |

## Contributing

1. Fork and clone the repository.
2. Create a feature branch.
3. Run `dotnet build` (and `dotnet test` on Windows) before submitting PRs.
4. Describe CLI/UX changes in PR description and update README if necessary.

## License

Textie is released under the [MIT License](LICENSE).

## Acknowledgements

- Spectre.Console team for an exceptional terminal toolkit.

- Vanara for modern P/Invoke wrappers used in keyboard input automation.
- The wider .NET community for tooling and inspiration.
