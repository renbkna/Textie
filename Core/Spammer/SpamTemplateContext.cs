using System;

namespace Textie.Core.Spammer
{
    public class SpamTemplateContext
    {
        public SpamTemplateContext(int index, int total, Random random)
        {
            Index = index;
            Total = total;
            Timestamp = DateTimeOffset.UtcNow;
            Random = random;
            MessageGuid = Guid.NewGuid();
        }

        public int Index { get; }
        public int Total { get; }
        public DateTimeOffset Timestamp { get; }
        public Random Random { get; }
        public Guid MessageGuid { get; }
    }
}
