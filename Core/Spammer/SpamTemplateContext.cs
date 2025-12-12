using System;

namespace Textie.Core.Spammer;

public sealed class SpamTemplateContext(int index, int total, Random random)
{
    public int Index { get; } = index;
    public int Total { get; } = total;
    public Random Random { get; } = random;
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    public Guid MessageGuid { get; } = Guid.NewGuid();
}
