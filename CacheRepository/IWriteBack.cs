using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CacheRepository
{
    // 说明：通过抽象出该接口，让cacherepository能够比较容易支持
    // 数据同步的功能；对，缓存中数据同步是通过WriteBack的方式来
    // 实现的；
    // BackWritter : IWriteBack
    public interface IWriteBack
    {
        Task<bool> SyncInsert(/*待定*/);
        Task<bool> SyncDelete(/*待定*/);
        Task<bool> SyncUpdate(Dictionary<string, object> tracedInfo);
    }

    public sealed class SyncerManager
    {
        private static readonly IWriteBack _default = new DefaultSyncer();
        private SyncerManager() { }
        public static IWriteBack DefaultSyncer { get { return _default; } }
    }

    public class DefaultSyncer : IWriteBack
    {
        public Task<bool> SyncDelete()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SyncInsert()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SyncUpdate(Dictionary<string, object> tracedInfo)
        {
            throw new NotImplementedException();
        }
    }
}
