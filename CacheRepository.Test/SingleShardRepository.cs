using System;
using System.Collections.Generic;

namespace CacheRepository.Test
{
    /// <summary>
    /// 按user对象的id同时来做为缓存的key，以及分片的键
    /// </summary>
    public class SingleShardRepository : CacheRepository<int, User, int>
    {
        public override Func<int, (int index, string tag)> GetShardingRule()
        {
            return p => (0, "默认分区");
        }

        public override int GetShardKey(User value)
        {
            return 0;
        }

        protected override int GetKey(User value)
        {
            return value.Id;
        }

        protected override List<User> GetRawData()
        {
            return new List<User>
            {
                new User { Id = 1, Age = 10, Name = "UserA", Level = 0 },
                new User { Id = 2, Age = 12, Name = "UserB", Level = 1 },
                new User { Id = 3, Age = 12, Name = "UserC", Level = 1 },
                new User { Id = 4, Age = 15, Name = "UserD", Level = 2 },
                new User { Id = 5, Age = 16, Name = "UserE", Level = 2 },
            };
        }
    }
}
