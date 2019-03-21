using System;

namespace CacheRepository.UnitOfWork
{
    public interface IUnitOfWork<TKey, TValue, TShardKey>
        where TValue : class, IEntity
    {
        IUnitOfWork<TKey, TValue, TShardKey> Begin(TShardKey shard);
        IUnitOfWork<TKey, TValue, TShardKey> AddItem(TKey key, TValue value);
        IUnitOfWork<TKey, TValue, TShardKey> GetItem(TKey key);
        IUnitOfWork<TKey, TValue, TShardKey> DoWithResult(Action<TValue> action);
        IUnitOfWork<TKey, TValue, TShardKey> DoWithResult(Func<TValue, TValue> func);
        IUnitOfWork<TKey, TValue, TShardKey> UpdateItem(TKey key, Action<TValue> update);
        IUnitOfWork<TKey, TValue, TShardKey> UpdateItem(TKey key, Func<TValue, TValue> update);
        IUnitOfWork<TKey, TValue, TShardKey> RemoveItem(TKey key);
        void Go();
    }
}
