using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Textie.Core.Abstractions;

namespace Textie.Core.Infrastructure;
    public sealed class NativeInputService : ITextAutomationService
    {
        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(text)) return Task.CompletedTask;

            var inputs = new INPUT[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                inputs[i * 2] = new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wScan = (ushort)text[i],
                            dwFlags = KEYEVENTF.UNICODE
                        }
                    }
                };

                inputs[i * 2 + 1] = new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wScan = (ushort)text[i],
                            dwFlags = KEYEVENTF.UNICODE | KEYEVENTF.KEYUP
                        }
                    }
                };
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            return Task.CompletedTask;
        }

        public async Task TypeTextAsync(string text, int perCharacterDelayMilliseconds, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(text)) return;

            var inputDown = new INPUT
            {
                type = InputType.INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        dwFlags = KEYEVENTF.UNICODE
                    }
                }
            };

            var inputUp = new INPUT
            {
                type = InputType.INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        dwFlags = KEYEVENTF.UNICODE | KEYEVENTF.KEYUP
                    }
                }
            };

            var inputs = new INPUT[2];

            foreach (char c in text)
            {
                cancellationToken.ThrowIfCancellationRequested();

                inputDown.U.ki.wScan = c;
                inputUp.U.ki.wScan = c;

                inputs[0] = inputDown;
                inputs[1] = inputUp;

                SendInput(2, inputs, Marshal.SizeOf<INPUT>());

                if (perCharacterDelayMilliseconds > 0)
                {
                    await Delay(perCharacterDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public Task PressEnterAsync(CancellationToken cancellationToken)
        {
            var inputs = new[]
            {
                new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0x0D, // VK_RETURN
                            dwFlags = 0
                        }
                    }
                },
                new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0x0D, // VK_RETURN
                            dwFlags = KEYEVENTF.KEYUP
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            return Task.CompletedTask;
        }

        private static async ValueTask Delay(int milliseconds, CancellationToken token)
        {
            // Simple Task.Delay for now as placeholder for the strategy logic handling high-res,
            // but loop optimization logic might handle the spinwait.
            // For per-character typing, Task.Delay is usually acceptable as these are user-perceptible delays (e.g. 50ms).
            // The high-res timing is more critical for the inter-message delay in the Engine.
            await Task.Delay(milliseconds, token).ConfigureAwait(false);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public InputType type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public KEYEVENTF dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private enum InputType : uint
        {
            INPUT_MOUSE = 0,
            INPUT_KEYBOARD = 1,
            INPUT_HARDWARE = 2
        }

        [Flags]
        private enum KEYEVENTF : uint
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            UNICODE = 0x0004,
            SCANCODE = 0x0008
        }
    }
