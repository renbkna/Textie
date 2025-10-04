using System;

namespace Textie.Core.Spammer
{
    public class SpamProgressEventArgs : EventArgs
    {
        public SpamProgressEventArgs(int current, int total, string status)
        {
            Current = current;
            Total = total;
            Status = status;
        }

        public int Current { get; }
        public int Total { get; }
        public string Status { get; }
    }
}
