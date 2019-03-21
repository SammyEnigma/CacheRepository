using System;
using System.Collections.Generic;
using System.Text;

namespace CacheRepository
{
    // 说明：通过抽象出该接口，让cacherepository能够比较容易支持
    // 数据同步的功能；对，缓存中数据同步是通过WriteBack的方式来
    // 实现的；
    // BackWritter : IWriteBack
    public interface IWriteBack
    {
        bool SyncInsert(/*待定*/);
        bool SyncDelete(/*待定*/);
        bool SyncUpdate(Dictionary<string, object> tracedInfo);     
    }

    public class RabbitMqSyncer : IWriteBack
    {
        public bool SyncDelete()
        {
            throw new NotImplementedException();
        }

        public bool SyncInsert()
        {
            throw new NotImplementedException();
        }

        public bool SyncUpdate(Dictionary<string, object> tracedInfo)
        {
            throw new NotImplementedException();
        }
    }
}
