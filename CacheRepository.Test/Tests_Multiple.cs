using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CacheRepository.Tests
{
    public class Tests_Multiple
    {
        private readonly MultipleShardsRepository _repository;

        public Tests_Multiple()
        {
            _repository = new MultipleShardsRepository();
            _repository.Init();
        }

        [Fact]
        public void 初始化完成_Shard数应为3个()
        {
            Assert.True(_repository.Shards.Count == 3);
        }

        [Fact]
        public void 初始化完成_Shard中需要包含指定的User对象()
        {
            Assert.Collection<User>(_repository[0].Cache.Select(p => p.Value).ToList(),
                item => Assert.Contains("UserA", item.Name),
                item => Assert.Contains("UserB", item.Name),
                item => Assert.Contains("UserI", item.Name),
                item => Assert.Contains("UserJ", item.Name));

            Assert.Collection<User>(_repository[1].Cache.Select(p => p.Value).ToList(),
                item => Assert.Contains("UserC", item.Name),
                item => Assert.Contains("UserD", item.Name),
                item => Assert.Contains("UserK", item.Name));

            Assert.Collection<User>(_repository[2].Cache.Select(p => p.Value).ToList(),
                item => Assert.Contains("UserE", item.Name),
                item => Assert.Contains("UserF", item.Name),
                item => Assert.Contains("UserG", item.Name),
                item => Assert.Contains("UserH", item.Name));
        }

        [Fact]
        public void 添加user对象_Level为6应该在shard0中()
        {
            _repository.Add("UserL", new User { Id = 12, Age = 10, Name = "UserL", Level = 6 });
            Assert.True(_repository[0].Cache.ContainsKey("UserL"));
        }

        [Fact]
        public void 添加user对象_Level为7应该在shard1中()
        {
            _repository.Add("UserM", new User { Id = 13, Age = 12, Name = "UserM", Level = 7 });
            Assert.True(_repository[1].Cache.ContainsKey("UserM"));
        }

        [Fact]
        public void 添加user对象_Level为8应该在shard2中()
        {
            _repository.Add("UserN", new User { Id = 14, Age = 12, Name = "UserN", Level = 8 });
            Assert.True(_repository[2].Cache.ContainsKey("UserN"));
        }

        [Fact]
        public void 添加user对象_相同键需要抛出异常()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _repository.Add("UserL", new User { Id = 12, Age = 10, Name = "UserL", Level = 6 });
                _repository.Add("UserL", new User { Id = 12, Age = 10, Name = "UserL", Level = 6 });
            });
        }

        [Fact]
        public void 获取user对象_错误的键需要抛出异常()
        {
            Assert.Throws<KeyNotFoundException>(() => _repository.Get("UserX", 6));
        }

        [Fact]
        public void 获取user对象_错误的分片键需要抛出异常()
        {
            Assert.Throws<ArgumentException>(() => _repository.Get("UserL", -1));
        }

        [Fact]
        public void 获取或创建_分片号不一致的情况需要抛出异常()
        {
            // 创建时指定分片键为Level=5，而factory创建的对象分片键为Level=6，因此分片不一致，抛出异常
            Assert.Throws<ArgumentException>(() => _repository.GetOrCreate("UserL",
                () => new User { Id = 12, Age = 10, Name = "UserL", Level = 6 }, 5));
        }

        [Fact]
        public void 尝试更新_分片键的更新导致对象移动分区1()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            _repository.TryUpdate(user.Name, user.Level, p => p.Level += 1);
            Assert.Collection<User>(_repository[0].Cache.Select(p => p.Value).ToList(),
                item => Assert.Contains("UserB", item.Name),
                item => Assert.Contains("UserI", item.Name),
                item => Assert.Contains("UserJ", item.Name));

            Assert.Collection<User>(_repository[1].Cache.Select(p => p.Value).ToList(),
                item => Assert.Contains("UserC", item.Name),
                item => Assert.Contains("UserD", item.Name),
                item => Assert.Contains("UserK", item.Name),
                item => Assert.Contains("UserA", item.Name));
        }

        [Fact]
        public void 尝试更新_分片键的更新导致对象移动分区2()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            _repository.TryUpdate(user.Name, user.Level, p => { p.Level += 1; return p; });
            Assert.Collection<User>(_repository[0].Cache.Select(p => p.Value).ToList(),
                item => Assert.Contains("UserB", item.Name),
                item => Assert.Contains("UserI", item.Name),
                item => Assert.Contains("UserJ", item.Name));

            Assert.Collection<User>(_repository[1].Cache.Select(p => p.Value).ToList(),
                item => Assert.Contains("UserC", item.Name),
                item => Assert.Contains("UserD", item.Name),
                item => Assert.Contains("UserK", item.Name),
                item => Assert.Contains("UserA", item.Name));
        }
    }
}
