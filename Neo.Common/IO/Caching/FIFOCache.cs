namespace Neo.IO.Caching
{
    public abstract class FIFOCache<TKey, TValue> : Cache<TKey, TValue>
    {
        public FIFOCache(int max_capacity)
            : base(max_capacity)
        {
        }

        protected override void OnAccess(CacheItem item)
        {
        }
    }
}
