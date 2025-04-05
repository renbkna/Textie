using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // Needed for Application.Run() message loop & SynchronizationContext
using Spectre.Console;
using WindowsInput; // For InputSimulator
using WindowsInput.Native; // For VirtualKeyCode

namespace TextSpammer
{
    static class Program
    {
        // Constants for the low-level keyboard hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        // Virtual key codes
        private const int VK_RETURN = 0x0D;  // Enter key
        private const int VK_ESCAPE = 0x1B;  // Escape key

        // Delegate and hook handle for keyboard hook
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero; // Store the hook handle

        // State flags
        private static volatile bool _isSpamming = false;
        private static CancellationTokenSource _cts;
        private static bool _runCancelled = false; // Flag to check if last run was cancelled

        // Spamming thread
        private static Thread spamThread;

        // User input storage
        private static string _spamMessage;
        private static int _spamCount;
        private static int _delay;

        // InputSimulator instance
        private static readonly InputSimulator simulator = new InputSimulator();

        // Event for signalling the main thread's message loop to exit.
        private static ManualResetEventSlim _exitSignal = new ManualResetEventSlim(false);

        [STAThread]
        static void Main()
        {
            bool changeConfig = true; // Start by collecting config
            bool firstRun = true;

            try
            {
                AnsiConsole.Write(new Rule("[bold yellow]Advanced Text Spammer[/]").Centered());
                AnsiConsole.WriteLine();

                while (true) // Main application loop
                {
                    if (changeConfig)
                    {
                        if (!firstRun) // Add spacing if not the very first time
                        {
                            AnsiConsole.WriteLine();
                            AnsiConsole.Write(new Rule("[blue]Change Configuration[/]").LeftJustified());
                        }
                        // --- Collect User Inputs ---
                        _spamMessage = AnsiConsole.Ask<string>("Enter the [green]message[/] you want to spam:", _spamMessage ?? ""); // Suggest previous value

                        _spamCount = AnsiConsole.Prompt(
                            new TextPrompt<int>("Enter the [green]number of times[/] to spam:")
                                .PromptStyle("green")
                                .DefaultValue(_spamCount > 0 ? _spamCount : 1) // Suggest previous value
                                .ValidationErrorMessage("[red]That's not a valid number[/]")
                                .Validate(count => count > 0 ? ValidationResult.Success() :
                                    ValidationResult.Error("[red]Please enter a positive number[/]")));

                        _delay = AnsiConsole.Prompt(
                            new TextPrompt<int>("Enter [green]delay[/] between messages in [blue]milliseconds[/]:")
                                .PromptStyle("green")
                                .DefaultValue(_delay >= 0 ? _delay : 0) // Suggest previous value
                                .ValidationErrorMessage("[red]That's not a valid number[/]")
                                .Validate(delay => delay >= 0 ? ValidationResult.Success() :
                                    ValidationResult.Error("[red]Please enter a non-negative number[/]")));

                        AnsiConsole.WriteLine();
                        changeConfig = false; // Config collected for this cycle
                        firstRun = false;
                    }

                    // --- Display Instructions ---
                    AnsiConsole.Write(
                        new Spectre.Console.Panel(
                            @"[bold]Instructions:[/]
- Press [yellow]Enter[/] to [green]start[/] spamming with current settings.
- Press [yellow]Escape[/] to [red]stop[/] spamming during execution.
- Ensure the [blue]target application[/] is [underline]focused[/].")
                        .Header("Ready")
                        .Border(BoxBorder.Rounded)
                        .Expand());
                    AnsiConsole.WriteLine();

                    // --- Setup Hook and Run Message Loop ---
                    _runCancelled = false; // Reset cancellation flag for this run
                    _exitSignal.Reset(); // Ensure signal is reset before starting loop

                    _hookID = SetHook(_proc); // Set the hook FOR THIS RUN
                    if (_hookID == IntPtr.Zero)
                    {
                        AnsiConsole.MarkupLine("[red]Error: Failed to set keyboard hook. Try running as administrator.[/]");
                        break; // Exit loop if hook fails
                    }

                    AnsiConsole.MarkupLine("[cyan]Waiting for key presses ([yellow]Enter[/] to start)...[/]");

                    // --- Manual Message Loop ---
                    // Replace Application.Run() with a loop that processes messages and waits for the signal
                    while (!_exitSignal.IsSet)
                    {
                        Application.DoEvents(); // Process Windows messages (needed for hook)
                        // Wait briefly to avoid pegging CPU, DoEvents might yield anyway
                        _exitSignal.Wait(TimeSpan.FromMilliseconds(10)); // Wait up to 10ms for signal
                    }
                    // --- Loop exited because _exitSignal was Set ---

                    AnsiConsole.MarkupLine("[grey]Message loop finished.[/]");

                    // --- After Run Completes (Application.Run returns) ---
                    // Hook MUST be released here before the prompt
                    if (_hookID != IntPtr.Zero)
                    {
                        UnhookWindowsHookEx(_hookID);
                        _hookID = IntPtr.Zero;
                        AnsiConsole.MarkupLine("[grey]Keyboard hook released for this cycle.[/]");
                    }

                    // --- Display Completion Status ---
                    if (_runCancelled)
                    {
                        AnsiConsole.MarkupLine("[yellow]Run stopped by user.[/]");
                    }
                    else if (!_isSpamming) // Check if it wasn't stopped *before* completing
                    {
                        AnsiConsole.MarkupLine("[green]Run completed.[/]");
                    }


                    // --- Ask User What's Next ---
                    AnsiConsole.WriteLine();
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("What next?")
                            .PageSize(4)
                            .AddChoices(new[] {
                                "Run again (same settings)",
                                "Change settings",
                                "Exit"
                            }));

                    if (choice == "Run again (same settings)")
                    {
                        changeConfig = false;
                        continue; // Loop back, keeping current config
                    }
                    else if (choice == "Change settings")
                    {
                        changeConfig = true;
                        continue; // Loop back, setting flag to collect config
                    }
                    else // Exit
                    {
                        break; // Exit the while loop
                    }
                } // End while(true) loop
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
            }
            finally
            {
                // Final cleanup: Ensure hook is definitely uninstalled if loop breaks unexpectedly
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    AnsiConsole.MarkupLine("[grey]Final keyboard hook release check.[/]");
                }
                AnsiConsole.MarkupLine("[bold blue]Exiting application.[/]");
            }
        }

        // Spamming logic executed on a separate thread.
        // Uses ManualResetEventSlim to signal completion.
        private static void StartSpamming(string message, int count, int delay, CancellationToken token)
        {
            _isSpamming = true;
            _runCancelled = false; // Reset flag at start

            AnsiConsole.MarkupLine("\n[green]Spamming started. Press 'Escape' to stop.[/]");

            try
            {
                AnsiConsole.Progress()
                    .Start(ctx =>
                    {
                        var task = ctx.AddTask("[blue]Spamming progress[/]", new ProgressTaskSettings { MaxValue = count, AutoStart = true });

                        for (int i = 0; i < count; i++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                _runCancelled = true; // Set cancellation flag
                                task.StopTask();
                                break;
                            }

                            SpamMessage(message);
                            task.Increment(1);
                            Thread.Sleep(delay);
                        }

                        if (!token.IsCancellationRequested)
                        {
                            task.Value = task.MaxValue; // Ensure bar fills completely
                        }
                    });
            }
            finally // Ensure state is reset and main loop is signalled
            {
                _isSpamming = false;
                // Signal the main thread's message loop to exit using the ManualResetEventSlim
                _exitSignal.Set();
            }
        }

        // Sends a single message and Enter key press via InputSimulator.
        private static void SpamMessage(string message)
        {
            try
            {
                simulator.Keyboard.TextEntry(message);
                simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"- [red]Error sending message: {ex.Message}[/]");
            }
        }

        // Sets the low-level keyboard hook.
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            // Check if already hooked (shouldn't happen with current loop structure, but good practice)
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
            }

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                if (curModule == null)
                {
                    AnsiConsole.MarkupLine("[red]Error: Cannot get current process module.[/]");
                    return IntPtr.Zero;
                }
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        // The hook callback that processes global key events.
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == VK_RETURN && !_isSpamming)
                {
                    // Reset the signal JUST before starting a new run
                    // Ensures the next loop iteration waits correctly
                    _exitSignal.Reset();

                    AnsiConsole.MarkupLine("[yellow]Enter key pressed.[/] [green]Starting spamming...[/]");
                    _cts = new CancellationTokenSource();
                    // Pass only needed parameters
                    spamThread = new Thread(() => StartSpamming(_spamMessage, _spamCount, _delay, _cts.Token))
                    {
                        IsBackground = true
                    };
                    spamThread.Start();
                }
                else if (vkCode == VK_ESCAPE && _isSpamming)
                {
                    AnsiConsole.MarkupLine("[yellow]Escape key pressed.[/] [red]Requesting stop...[/]");
                    _cts?.Cancel(); // Cancellation token is handled, finally block will Set the signal
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // --- P/Invoke Signatures for Windows API --- //

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
