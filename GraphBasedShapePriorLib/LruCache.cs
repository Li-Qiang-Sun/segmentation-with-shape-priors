using System;
using System.Collections.Generic;

namespace Research.GraphBasedShapePrior
{
    public class LruCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, LinkedListNode<StorageItem>> keyToStorage =
            new Dictionary<TKey, LinkedListNode<StorageItem>>();

        private readonly LinkedList<StorageItem> storage = new LinkedList<StorageItem>();

        public LruCache(int capacity)
        {
            for (int i = 0; i < capacity; ++i)
                storage.AddLast(new StorageItem());
        }

        public void Add(TKey key, TValue value)
        {
            LinkedListNode<StorageItem> storageNode = storage.Last;
            if (keyToStorage.Count == storage.Count)
            {
                keyToStorage.Remove(storageNode.Value.Key);
                CacheItemDiscarded(this, new LruCacheItemDiscardedEventArgs<TKey, TValue>(storageNode.Value.Key, storageNode.Value.Value));
            }
            
            storageNode.Value.Key = key;
            storageNode.Value.Value = value;
            keyToStorage.Add(key, storageNode);
            Touch(storageNode);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            LinkedListNode<StorageItem> storageNode;
            if (!keyToStorage.TryGetValue(key, out storageNode))
                return false;
            value = storageNode.Value.Value;
            Touch(storageNode);
            return true;
        }

        public event EventHandler<LruCacheItemDiscardedEventArgs<TKey, TValue>> CacheItemDiscarded;

        private void Touch(LinkedListNode<StorageItem> storageNode)
        {
            storage.Remove(storageNode);
            storage.AddFirst(storageNode);
        }

        private class StorageItem
        {
            public TKey Key { get; set; }

            public TValue Value { get; set; }
        }
    }
}