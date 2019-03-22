using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CacheRepository
{
    // 说明：通过抽象出该接口，让cacherepository能够比较容易支持
    // 数据同步的功能；对，缓存中数据同步是通过WriteBack的方式来
    // 实现的；
    // BackWritter : IWriteBack
    public interface IWriteBack<TValue>
        where TValue : class, IEntity
    {
        Task<bool> SyncInsert(/*待定*/);
        Task<bool> SyncDelete(/*待定*/);
        Task<bool> SyncUpdate(Dictionary<string, object> tracedInfo);
    }

    public sealed class SyncerManager
    {
        private SyncerManager() { }
        // syncer的类型应该是跟着TValue走的，所以一定会需要一个类型参数<TValue>
        public static IWriteBack<TValue> DefaultSyncer<TValue>()
            where TValue : class, IEntity
        {
            return Nested<TValue>.instance;
        }

        // 仅仅是为了不让类型参数<TValue>出现在SyncerManager一层，没有特别的意义
        private sealed class Nested<TValue>
            where TValue : class, IEntity
        {
            public static readonly IWriteBack<TValue> instance = new DefaultSyncer<TValue>();
        }
    }

    public sealed class DefaultSyncer<TValue> : IWriteBack<TValue>
        where TValue : class, IEntity
    {
        public Task<bool> SyncDelete()
        {
            return Task.FromResult(true);
        }

        public Task<bool> SyncInsert()
        {
            return Task.FromResult(true);
        }

        public Task<bool> SyncUpdate(Dictionary<string, object> tracedInfo)
        {
            return Task.FromResult(true);
        }
    }
}
