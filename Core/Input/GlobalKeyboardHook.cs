using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Textie.Core.Abstractions;

namespace Textie.Core.Input
{
    public sealed class GlobalKeyboardHook : IHotkeyService
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int VkReturn = 0x0D;
        private const int VkEscape = 0x1B;

        private readonly ILogger<GlobalKeyboardHook> _logger;
        private LowLevelKeyboardProc? _hookProc;
        private IntPtr _hookId = IntPtr.Zero;
        private Thread? _hookThread;
        private uint _hookThreadId;
        private volatile bool _stopRequested;
        private TaskCompletionSource<HotkeyAction>? _signalSource;
        private bool _disposed;

        public GlobalKeyboardHook(ILogger<GlobalKeyboardHook> logger)
        {
            _logger = logger;
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            EnsureHookThreadStarted();
            return Task.CompletedTask;
        }

        public Task<HotkeyAction> WaitForNextAsync(CancellationToken cancellationToken)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GlobalKeyboardHook));

            EnsureHookThreadStarted();

            var source = new TaskCompletionSource<HotkeyAction>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));

            var previous = Interlocked.Exchange(ref _signalSource, source);
            previous?.TrySetResult(HotkeyAction.None);

            return WaitForCompletionAsync(source, registration);

            async Task<HotkeyAction> WaitForCompletionAsync(TaskCompletionSource<HotkeyAction> completionSource, CancellationTokenRegistration tokenRegistration)
            {
                try
                {
                    return await completionSource.Task.ConfigureAwait(false);
                }
                finally
                {
                    tokenRegistration.Dispose();
                    Interlocked.CompareExchange(ref _signalSource, null, completionSource);
                }
            }
        }

        public void SignalCompletion()
        {
            _signalSource?.TrySetResult(HotkeyAction.None);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WmKeydown)
            {
                try
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    switch (vkCode)
                    {
                        case VkReturn:
                            _signalSource?.TrySetResult(HotkeyAction.Start);
                            break;
                        case VkEscape:
                            _signalSource?.TrySetResult(HotkeyAction.Stop);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling keyboard hook callback.");
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;

            if (curModule?.ModuleName is null)
            {
                return IntPtr.Zero;
            }

            var hookId = SetWindowsHookEx(
                WhKeyboardLl,
                proc,
                GetModuleHandle(curModule.ModuleName),
                0);

            if (hookId == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to set keyboard hook. Win32 Error Code: {ErrorCode}", errorCode);
            }

            return hookId;
        }

        private void EnsureHookThreadStarted()
        {
            if (_hookThread != null)
            {
                return;
            }

            _stopRequested = false;
            _hookThread = new Thread(HookThreadProc)
            {
                IsBackground = true,
                Name = "Textie.KeyboardHook"
            };
            _hookThread.Start();
        }

        private void HookThreadProc()
        {
            _hookThreadId = GetCurrentThreadId();
            _hookProc ??= HookCallback;

            _hookId = SetHook(_hookProc);
            if (_hookId == IntPtr.Zero)
            {
                return;
            }

            try
            {
                while (!_stopRequested)
                {
                    if (GetMessage(out var msg, IntPtr.Zero, 0, 0) <= 0)
                    {
                        break; // WM_QUIT or error
                    }
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
            finally
            {
                var handle = Interlocked.Exchange(ref _hookId, IntPtr.Zero);
                if (handle != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(handle);
                }
            }
        }

        private void EnsureHookInstalled()
        {
            if (_hookId != IntPtr.Zero)
            {
                return;
            }

            _hookProc ??= HookCallback;
            _hookId = SetHook(_hookProc);
            if (_hookId == IntPtr.Zero)
            {
                 int errorCode = Marshal.GetLastWin32Error();
                 throw new InvalidOperationException($"Failed to install global keyboard hook. Win32 Error: {errorCode}");
            }
        }

        private void UninstallHook()
        {
            var handle = Interlocked.Exchange(ref _hookId, IntPtr.Zero);
            if (handle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(handle);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _stopRequested = true;
            if (_hookThreadId != 0)
            {
                PostThreadMessage(_hookThreadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
            }
            try
            {
                _hookThread?.Join(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // ignore
            }

            _signalSource?.TrySetCanceled();
            _signalSource = null;
            _disposed = true;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WmQuit = 0x0012;

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }
    }
}
