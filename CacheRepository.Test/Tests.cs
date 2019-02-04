using System;
using System.Linq;
using Xunit;

namespace CacheRepository.Test
{
    public class Tests
    {
        private readonly SingleShardRepository _repository;

        public Tests()
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
    }
}
