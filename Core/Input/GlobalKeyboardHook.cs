using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Textie.Core.Abstractions;

namespace Textie.Core.Input
{
    public sealed class GlobalKeyboardHook : IHotkeyService
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int VkReturn = 0x0D;
        private const int VkEscape = 0x1B;

        private LowLevelKeyboardProc? _hookProc;
        private IntPtr _hookId = IntPtr.Zero;
        private TaskCompletionSource<HotkeyAction>? _signalSource;
        private bool _disposed;

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<HotkeyAction> WaitForNextAsync(CancellationToken cancellationToken)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GlobalKeyboardHook));

            EnsureHookInstalled();

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
                    UninstallHook();
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
                catch
                {
                    // ignore hook errors to avoid destabilizing host
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

            return SetWindowsHookEx(
                WhKeyboardLl,
                proc,
                GetModuleHandle(curModule.ModuleName),
                0);
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
                throw new InvalidOperationException("Failed to install global keyboard hook. Consider running as administrator.");
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

            UninstallHook();
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
    }
}
