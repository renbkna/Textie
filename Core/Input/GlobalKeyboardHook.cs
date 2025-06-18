using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Textie.Core.Input
{
    public class GlobalKeyboardHook : IDisposable
    {
        // Windows API constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_RETURN = 0x0D;
        private const int VK_ESCAPE = 0x1B;

        // Events
        public event Action? EnterPressed;
        public event Action? EscapePressed;

        // Hook management
        private LowLevelKeyboardProc _hookProc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool _disposed = false;

        // Async synchronization
        private readonly ManualResetEventSlim _completionSignal = new(false);

        public GlobalKeyboardHook()
        {
            _hookProc = HookCallback;
        }

        public bool Install()
        {
            if (_hookId != IntPtr.Zero)
                return true; // Already installed

            _hookId = SetHook(_hookProc);
            return _hookId != IntPtr.Zero;
        }

        public void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        public async Task WaitForNextActionAsync()
        {
            _completionSignal.Reset();

            // Run message loop in a task to keep it responsive
            await Task.Run(() =>
            {
                while (!_completionSignal.IsSet)
                {
                    Application.DoEvents();
                    _completionSignal.Wait(TimeSpan.FromMilliseconds(10));
                }
            });
        }

        public void SignalCompletion()
        {
            _completionSignal.Set();
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            try
            {
                using var curProcess = Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;

                if (curModule?.ModuleName == null)
                    return IntPtr.Zero;

                return SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    proc,
                    GetModuleHandle(curModule.ModuleName),
                    0);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);

                    switch (vkCode)
                    {
                        case VK_RETURN:
                            EnterPressed?.Invoke();
                            break;
                        case VK_ESCAPE:
                            EscapePressed?.Invoke();
                            break;
                    }
                }
            }
            catch
            {
                // Ignore exceptions in hook callback to prevent system instability
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Uninstall();
                _completionSignal?.Dispose();
                _disposed = true;
            }
        }

        // Windows API P/Invoke declarations
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

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
