# Textie

A Windows text automation tool built with .NET that allows you to send repeated messages to any application.

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/OS-Windows-blue.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Features

- **Global keyboard hooks** - Start/stop with Enter/Escape keys from anywhere
- **Input simulation** - Sends text to any focused Windows application
- **Configurable timing** - Control delay between messages (0ms to 60 seconds)
- **Progress tracking** - Real-time progress display with cancellation support
- **Input validation** - Comprehensive validation with helpful feedback
- **Configuration management** - Remembers settings between runs

## Quick Start

1. **Prerequisites**: Windows 10/11 with .NET 9.0 runtime
2. **Clone and run**:

   ```bash
   git clone https://github.com/yourusername/textie.git
   cd textie
   dotnet run
   ```

3. **Configure** your message, repetition count, and timing
4. **Focus** the target application where you want to send text
5. **Press Enter** to start, **Escape** to stop

## Usage

### Basic Operation

1. Run the application
2. Enter your message text
3. Set number of repetitions (1-10,000)
4. Choose timing interval or set custom delay
5. Review configuration and confirm
6. Switch to target application
7. Press Enter to begin automation

### Timing Options

- **Instant (0ms)** - No delay between messages
- **Rapid (50ms)** - Fast automation for responsive apps
- **Standard (100ms)** - Good balance for most use cases
- **Moderate (500ms)** - Safer for slower applications
- **Deliberate (1000ms)** - Very conservative timing
- **Custom** - Specify exact millisecond delay

### Safety Features

- Automatic confirmation prompts for large operations
- Real-time validation with impact assessment
- Emergency stop with Escape key
- Input sanitization and length limits

## Architecture

The application uses a modular architecture with clear separation of concerns:

```
├── Program.cs                    # Application entry point
└── Core/
    ├── TextieApplication.cs      # Main application coordinator
    ├── Configuration/            # Settings and validation
    ├── UI/                       # Console interface
    ├── Input/                    # Keyboard hook management
    └── Spammer/                  # Text automation engine
```

### Key Components

- **Configuration Management** - Type-safe settings with validation
- **User Interface** - Professional console UI using Spectre.Console
- **Global Input Handling** - Windows API keyboard hooks
- **Text Automation Engine** - Multi-threaded message sending with progress tracking

## Development

### Building from Source

```bash
git clone https://github.com/yourusername/textie.git
cd textie
dotnet build
dotnet run
```

### Project Structure

- **Clean Architecture** - Modular design with dependency injection
- **Event-Driven** - Loose coupling between components
- **Async/Await** - Non-blocking operations throughout
- **Resource Management** - Proper cleanup and disposal patterns

### Dependencies

- [Spectre.Console](https://spectreconsole.net/) - Rich console applications
- [InputSimulatorPlus](https://github.com/GregsStack/InputSimulatorPlus) - Windows input simulation

## Configuration

Settings are validated in real-time with helpful feedback:

| Setting | Range | Description |
|---------|-------|-------------|
| Message | 1-1000 chars | Text to send to target application |
| Count | 1-10,000 | Number of times to repeat the message |
| Delay | 0-60,000ms | Pause between each message |

Large operations (100+ messages or fast timing) require confirmation.

## Troubleshooting

**Keyboard hooks not working?**

- Try running as Administrator
- Some security software may block low-level hooks

**Text not appearing in target app?**

- Ensure the target application has focus
- Some games/protected apps may block simulated input
- Try increasing the delay between messages

**Application hangs or crashes?**

- Use Escape key to emergency stop
- Restart the application if needed
- Check Windows Event Viewer for error details

## Legal Notice

**Educational and Testing Use Only**

This software is intended for educational purposes, automation testing, and productivity enhancement. Users are responsible for complying with:

- Target application terms of service
- Local laws and regulations
- Platform-specific automation policies

Misuse including harassment, spam, or violation of service terms is prohibited.

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

### Areas for Improvement

- Cross-platform support
- GUI interface option
- Advanced scripting capabilities
- Plugin system
- Enhanced targeting options

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **Spectre.Console** team for the excellent console framework
- **InputSimulatorPlus** contributors for reliable input simulation
- The .NET community for tools and guidance
