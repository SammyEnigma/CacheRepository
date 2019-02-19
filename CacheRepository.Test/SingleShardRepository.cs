using System;
using System.Collections.Generic;

namespace CacheRepository.Tests
{
    /// <summary>
    /// 按user对象的id同时来做为缓存的key，以及分片的键
    /// </summary>
    public class SingleShardRepository : CacheRepository<int, User, int>
    {
        public override Func<int, (int index, string tag)> GetShardingRule()
        {
            return p => (0, "默认分片");
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
                new User { Id = 5, Age = 16, Name = "UserE", Level = 2 }
            };
        }

        public new bool Add(int key, User value)
        {
            return base.Add(key, value);
        }

        public new User Get(int key, int shard, bool deepClone = true)
        {
            return base.Get(key, shard, deepClone);
        }

        public new bool TryGet(int key, out User value, int shard, bool deepClone = true)
        {
            return base.TryGet(key, out value, shard, deepClone);
        }

        public new User GetOrCreate(int key, User value, bool deepClone = true)
        {
            return base.GetOrCreate(key, value, deepClone);
        }

        public new User GetOrCreate(int key, Func<User> factory, int shard, bool deepClone = true)
        {
            return base.GetOrCreate(key, factory, shard, deepClone);
        }

        public new bool TryUpdate(int key, int shard, Action<User> update)
        {
            return base.TryUpdate(key, shard, update);
        }

        public new bool TryUpdate(int key, int shard, Func<User, User> update)
        {
            return base.TryUpdate(key, shard, update);
        }

        public new bool Remove(int key, int shard)
        {
            return base.Remove(key, shard);
        }

        public new bool ContainsKey(int key, int shard)
        {
            return base.ContainsKey(key, shard);
        }
    }
}
