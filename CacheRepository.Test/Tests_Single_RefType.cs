using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CacheRepository.Tests
{
    public class Tests_Single_RefType
    {
        private readonly SingleShardRepository _repository;

        public Tests_Single_RefType()
        {
            _repository = new SingleShardRepository();
            _repository.Init();
        }

        [Fact]
        public void 初始化完成_Shard数应为1个()
        {
            Assert.True(_repository.Shards.Count == 1);
        }

        [Fact]
        public void 初始化完成_Shard0中需要包含5个指定的User对象()
        {
            Assert.Collection<User>(_repository[0].Cache.Select(p => p.Value).ToList(),
               item => Assert.Contains("UserA", item.Name),
               item => Assert.Contains("UserB", item.Name),
               item => Assert.Contains("UserC", item.Name),
               item => Assert.Contains("UserD", item.Name),
               item => Assert.Contains("UserE", item.Name));
        }

        [Fact]
        public void 添加user对象_id为6也应该在shard0中()
        {
            _repository.Add(6, new User { Id = 6, Age = 10, Name = "UserF", Level = 0 });
            Assert.True(_repository[0].Cache.ContainsKey(6));
        }

        [Fact]
        public void 添加user对象_相同键需要抛出异常()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _repository.Add(6, new User { Id = 6, Age = 10, Name = "UserF", Level = 0 });
                _repository.Add(6, new User { Id = 6, Age = 10, Name = "UserF", Level = 0 });
            });
        }

        [Fact]
        public void 获取user对象_错误的键需要抛出异常()
        {
            Assert.Throws<KeyNotFoundException>(() => _repository.Get(6, 100));
        }

        [Fact]
        public void 获取user对象_任意分片键都应该能找到同一个对象()
        {
            var user1 = _repository.Get(1, 100);
            var user2 = _repository.Get(1, -2);
            Assert.True(user1.Name == "UserA" && user2.Name == "UserA");
        }

        [Fact]
        public void 获取user对象_不使用深拷贝应该获取到同一个对象()
        {
            var user1 = _repository.Get(1, 100, false);
            var user2 = _repository.Get(1, -2, false);
            Assert.Same(user1, user2);
            user1.Age = 100;
            Assert.True(user2.Age == 100);
        }

        [Fact]
        public void 尝试获取user对象_错误的键应该返回false并且out参数为空()
        {
            User user;
            var ret = _repository.TryGet(6, out user, 100);
            Assert.False(ret);
            Assert.Null(user);
        }

        [Fact]
        public void 尝试获取user对象_不使用深拷贝应该获取到同一个对象()
        {
            User user1;
            var ret1 = _repository.TryGet(1, out user1, 100);
            User user2;
            var ret2 = _repository.TryGet(1, out user2, -2);
            Assert.True(ret1);
            Assert.True(ret2);
            Assert.True(user1.Name == "UserA" && user2.Name == "UserA");
        }

        [Fact]
        public void 尝试获取user对象_任意分片键都应该能找到同一个对象()
        {
            User user1;
            var ret1 = _repository.TryGet(1, out user1, 100, false);
            User user2;
            var ret2 = _repository.TryGet(1, out user2, -2, false);
            Assert.True(ret1);
            Assert.True(ret2);
            Assert.Same(user1, user2);
            user1.Age = 100;
            Assert.True(user2.Age == 100);
        }

        [Fact]
        public void 获取或创建_已有键应该返回已缓存对象()
        {
            var user1 = _repository.GetOrCreate(1, null, false);
            var user2 = _repository.Get(1, 100, false);
            Assert.Same(user1, user2);
        }

        [Fact]
        public void 获取或创建_不存在键需要即时创建对象并插入缓存中()
        {
            var user1 = new User { Id = 6, Age = 10, Name = "UserF", Level = 0 };
            var user2 = _repository.GetOrCreate(6, user1, false);
            Assert.Same(user1, user2);
        }
    }
}
