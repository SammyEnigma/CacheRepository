using System;

namespace CacheRepository
{
    public interface IShardable<TKey, TValue, TShardKey> : IRepository<TKey>
        where TValue : class
    {
        TShardKey GetShardKey(TValue value);
        Func<TShardKey, (int index, string tag)> GetShardingRule();
        Shard<TKey, TValue, TShardKey> this[int index] { get; }
    }
}
