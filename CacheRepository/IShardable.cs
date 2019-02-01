using System;

namespace CacheRepository
{
    public interface IShardable<TKey, TValue, TShardKey>
    {
        TShardKey GetShardKey(TValue value);
        Func<TShardKey, (int index, string tag)> GetShardingRule();
        Shard<TKey, TValue, TShardKey> this[int index] { get; }
    }
}
