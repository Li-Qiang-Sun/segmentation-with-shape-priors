using System;

namespace Research.GraphBasedShapePrior.Util
{
    public class LruCacheItemDiscardedEventArgs <TKey, TValue> : EventArgs
    {
        public TKey DiscardedKey { get; private set; }

        public TValue DiscardedValue { get; private set; }

        public LruCacheItemDiscardedEventArgs(TKey discardedKey, TValue discardedValue)
        {
            this.DiscardedKey = discardedKey;
            this.DiscardedValue = discardedValue;
        }
    }
}
