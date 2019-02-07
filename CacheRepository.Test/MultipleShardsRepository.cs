using System;
using System.Collections.Generic;

namespace CacheRepository.Tests
{
    /// <summary>
    /// 按user对象的name来作key，并使用level作为分片的键
    /// </summary>
    public class MultipleShardsRepository : CacheRepository<string, User, int>
    {
        public override Func<int, (int index, string tag)> GetShardingRule()
        {
            return p => (p % 3, "分片" + p % 3);
        }

        public override int GetShardKey(User value)
        {
            return value.Level;
        }

        protected override string GetKey(User value)
        {
            return value.Name;
        }

        protected override List<User> GetRawData()
        {
            /*  Level % 3
             *  
             *  Shard0      Shard1      Shard2
             *  A   0       C   1       E   2
             *  B   0       D   1       F   2
             *  I   3       K   1       G   2
             *  J   3                   H   2
             *  
             */
            return new List<User>
            {
                new User { Id = 1, Age = 10, Name = "UserA", Level = 0 },
                new User { Id = 2, Age = 10, Name = "UserB", Level = 0 },
                new User { Id = 3, Age = 11, Name = "UserC", Level = 1 },
                new User { Id = 4, Age = 12, Name = "UserD", Level = 1 },
                new User { Id = 5, Age = 12, Name = "UserE", Level = 2 },
                new User { Id = 6, Age = 12, Name = "UserF", Level = 2 },
                new User { Id = 7, Age = 12, Name = "UserG", Level = 2 },
                new User { Id = 8, Age = 13, Name = "UserH", Level = 2 },
                new User { Id = 9, Age = 15, Name = "UserI", Level = 3 },
                new User { Id = 10, Age = 16, Name = "UserJ", Level = 3 },
                new User { Id = 11, Age = 16, Name = "UserK", Level = 1 }
            };
        }
    }
}
