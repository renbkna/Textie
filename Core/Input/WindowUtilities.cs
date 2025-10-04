using System;
using System.Text;
using Vanara.PInvoke;

namespace Textie.Core.Input
{
    public static class WindowUtilities
    {
        public static string? GetForegroundWindowTitle()
        {
            var handle = User32.GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var buffer = new StringBuilder(512);
            var length = User32.GetWindowText(handle, buffer, buffer.Capacity);
            if (length <= 0)
            {
                return null;
            }

            return buffer.ToString();
        }
    }
}
