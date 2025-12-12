using System;

namespace Textie.Core.Spammer;

public class SpamRunSummary
{
    public int MessagesSent { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Cancelled { get; set; }
    public int Errors { get; set; }
    public bool FocusLost { get; set; }
}
