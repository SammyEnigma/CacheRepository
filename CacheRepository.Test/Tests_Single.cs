using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CacheRepository.Tests
{
    public class Tests_Single
    {
        private readonly SingleShardRepository _repository;

        public Tests_Single()
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

        [Fact]
        public void 获取或创建_factory返回null值应该抛出异常()
        {
            Assert.Throws<ArgumentException>(() => _repository.GetOrCreate(6, () => null, 100));
        }

        [Fact]
        public void 尝试修改_任意分片键都应该能修改指定对象()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            _repository.TryUpdate(user.Id, 100, p => p.Age += 1);
            _repository.TryUpdate(user.Id, 2000, p => p.Age += 1);

            Assert.True(_repository.Get(user.Id, 300).Age == 12);
        }

        [Fact]
        public void 尝试修改_错误的键更新将会失败()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            Assert.False(_repository.TryUpdate(6, 100, p => p.Age += 1));
        }

        [Fact]
        public void 删除_任意分片键应该都能删除指定对象()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            var ret = _repository.Remove(user.Id, -200);
            Assert.True(ret);
            Assert.Collection<User>(_repository[0].Cache.Select(p => p.Value).ToList(),
               item => Assert.Contains("UserB", item.Name),
               item => Assert.Contains("UserC", item.Name),
               item => Assert.Contains("UserD", item.Name),
               item => Assert.Contains("UserE", item.Name));
        }

        [Fact]
        public void 删除_一个不存在的键不会抛出异常而是返回失败()
        {
            var ret = _repository.Remove(6, -200);
            Assert.False(ret);
            Assert.Collection<User>(_repository[0].Cache.Select(p => p.Value).ToList(),
               item => Assert.Contains("UserA", item.Name),
               item => Assert.Contains("UserB", item.Name),
               item => Assert.Contains("UserC", item.Name),
               item => Assert.Contains("UserD", item.Name),
               item => Assert.Contains("UserE", item.Name));
        }

        [Fact]
        public void 是否存在键_任意分片都能检测是否存在()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            var ret1 = _repository.ContainsKey(user.Id, 100);
            var ret2 = _repository.ContainsKey(user.Id, -200);
            Assert.True(ret1);
            Assert.True(ret2);
        }

        [Fact]
        public void 是否存在键_正常存在的情况返回成功()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            var ret = _repository.ContainsKey(user.Id, 100);
            Assert.True(ret);
        }

        [Fact]
        public void 是否存在键_不存在的情况返回失败()
        {
            var user = new User { Id = 1, Age = 10, Name = "UserA", Level = 0 };
            var ret = _repository.ContainsKey(6, 100);
            Assert.False(ret);
        }

        [Fact]
        public void UnitOfWork_一次begin线程TLS应该正常初始化()
        {
            _repository.Begin(100);
            Assert.Equal(0, _repository.TLS_Key);
            Assert.Null(_repository.TLS_PreResult);
            Assert.Equal(0, _repository.TLS_ShardIndex);
            Assert.Equal("默认分片", _repository.TLS_ShardTag);
        }

        [Fact]
        public void UnitOfWork_两次begin抛出异常()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.Begin(100).Begin(100));
        }

        [Fact]
        public void UnitOfWork_直接调用AddItem抛出异常()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.AddItem(1, null));
        }

        [Fact]
        public void UnitOfWork_直接调用GetItem抛出异常()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.GetItem(1));
        }

        [Fact]
        public void UnitOfWork_直接调用DoWithResult抛出异常1()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.DoWithResult(p => p.Age = 100));
        }

        [Fact]
        public void UnitOfWork_直接调用DoWithResult抛出异常2()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.DoWithResult(p => p));
        }

        [Fact]
        public void UnitOfWork_直接调用UpdateItem抛出异常1()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.UpdateItem(1, p => p.Age = 100));
        }

        [Fact]
        public void UnitOfWork_直接调用UpdateItem抛出异常2()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.UpdateItem(1, p => p));
        }

        [Fact]
        public void UnitOfWork_直接调用RemoveItem抛出异常()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.RemoveItem(1));
        }

        [Fact]
        public void UnitOfWork_直接调用Go抛出异常()
        {
            Assert.Throws<InvalidOperationException>(() => _repository.Go());
        }

        [Fact]
        public void UnitOfWork_业务1()
        {
            var user = new User { Id = 6, Age = 11, Name = "UserF", Level = 2 };
            _repository.Begin(100)
                .AddItem(6, user)
                .GetItem(1)
                .DoWithResult(p => p.Age += 7)
                .RemoveItem(3)
                .Go();

            Assert.Collection<User>(_repository[0].Cache.Select(p => p.Value).ToList(),
               item => Assert.Contains("UserA", item.Name),
               item => Assert.Contains("UserB", item.Name),
               item => Assert.Contains("UserD", item.Name),
               item => Assert.Contains("UserE", item.Name),
               item => Assert.Contains("UserF", item.Name));

            Assert.True(_repository.Get(1, 100).Age == 17);
        }
    }
}
