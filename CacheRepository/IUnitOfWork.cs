using System;

namespace CacheRepository
{
    public interface IUnitOfWork<TKey, TValue, TShardKey>
    {
        IUnitOfWork<TKey, TValue, TShardKey> Begin(TKey key, TShardKey shard);
        IUnitOfWork<TKey, TValue, TShardKey> AddItem(TValue value);
        IUnitOfWork<TKey, TValue, TShardKey> GetItem();
        IUnitOfWork<TKey, TValue, TShardKey> DoWithResult(Action<TValue> action);
        IUnitOfWork<TKey, TValue, TShardKey> DoWithResult(Func<TValue, TValue> func);
        IUnitOfWork<TKey, TValue, TShardKey> UpdateItem(Action<TValue> update);
        IUnitOfWork<TKey, TValue, TShardKey> UpdateItem(Func<TValue, TValue> update);
        IUnitOfWork<TKey, TValue, TShardKey> RemoveItem();
        void Go();
    }
}
