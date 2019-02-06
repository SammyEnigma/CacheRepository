using System;
using System.Collections.Generic;
using System.Threading;

namespace CacheRepository
{
    public class CacheRepository
    {
    }

    public abstract class CacheRepository<TKey, TValue, TShardKey> :
        CacheRepository,
        IShardable<TKey, TValue, TShardKey>,
        IUnitOfWork<TKey, TValue, TShardKey>
        where TValue : class
    {
        private class TLS_UnitOfWork
        {
            public TKey Key;
            public TValue PreResult;
            public int ShardIndex;
            public string ShardTag;
        }

        private Dictionary<int, Shard<TKey, TValue, TShardKey>> _shardings;
        private ThreadLocal<TLS_UnitOfWork> _tls = new ThreadLocal<TLS_UnitOfWork>();
        public readonly Func<TShardKey, (int index, string tag)> Sharding;

        public CacheRepository()
        {
            _shardings = new Dictionary<int, Shard<TKey, TValue, TShardKey>>();
            Sharding = GetShardingRule();
        }

        #region ForTestOnly
        public Dictionary<int, Shard<TKey, TValue, TShardKey>> Shards { get => this._shardings; }
        public TKey TLS_Key { get => this._tls.Value.Key; }
        public TValue TLS_PreResult { get => this._tls.Value.PreResult; }
        public int TLS_ShardIndex { get => this._tls.Value.ShardIndex; }
        public string TLS_ShardTag { get => this._tls.Value.ShardTag; }
        #endregion

        protected abstract TKey GetKey(TValue value);

        protected abstract List<TValue> GetRawData();

        public virtual void Init()
        {
            // init 执行代码
            var raw_data = GetRawData();
            foreach (var item in raw_data)
            {
                var key = GetKey(item);
                var shard_key = GetShardKey(item);
                var (index, tag) = Sharding(shard_key);
                if (!_shardings.ContainsKey(index))
                {
                    _shardings[index] = new Shard<TKey, TValue, TShardKey>(index, tag, this);
                }
                _shardings[index].Cache.Add(key, item);
            }
        }

        public Shard<TKey, TValue, TShardKey> this[int index]
        {
            get
            {
                return _shardings[index];
            }
        }

        public abstract TShardKey GetShardKey(TValue value);

        public abstract Func<TShardKey, (int index, string tag)> GetShardingRule();

        public void Add(TKey key, TValue value)
        {
            var shard_key = GetShardKey(value);
            var (index, tag) = Sharding(shard_key);
            _shardings[index].Add(key, value);
        }

        public TValue Get(TKey key, TShardKey shard, bool deepClone = true)
        {
            var (index, tag) = Sharding(shard);
            return _shardings[index].Get(key, deepClone);
        }

        public bool TryGet(TKey key, out TValue value, TShardKey shard, bool deepClone = true)
        {
            var (index, tag) = Sharding(shard);
            return _shardings[index].TryGet(key, out value, deepClone);
        }

        public TValue GetOrCreate(TKey key, TValue value, bool deepClone = true)
        {
            return GetOrCreate(key, () => value, GetShardKey(value), deepClone);
        }

        public TValue GetOrCreate(TKey key, Func<TValue> factory, TShardKey shard, bool deepClone = true)
        {
            var (index, tag) = Sharding(shard);
            return _shardings[index].GetOrCreate(key, factory, deepClone);
        }

        public bool TryUpdate(TKey key, TShardKey shard, Action<TValue> update)
        {
            var (index, tag) = Sharding(shard);
            return _shardings[index].TryUpdate(key, update);
        }

        public bool TryUpdate(TKey key, TShardKey shard, Func<TValue, TValue> update)
        {
            var (index, tag) = Sharding(shard);
            return _shardings[index].TryUpdate(key, update);
        }

        public bool Remove(TKey key, TShardKey shard)
        {
            var (index, tag) = Sharding(shard);
            return _shardings[index].Remove(key);
        }

        public bool ContainsKey(TKey key, TShardKey shard)
        {
            var (index, tag) = Sharding(shard);
            return _shardings[index].ContainsKey(key);
        }

        #region unitofwork
        public IUnitOfWork<TKey, TValue, TShardKey> Begin(TShardKey shard/*在哪一个分区上执行此'事务'*/)
        {
            if (_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储已经有值");
            _tls.Value = new TLS_UnitOfWork();

            var (index, tag) = Sharding(shard);
            _tls.Value.ShardIndex = index;
            _tls.Value.ShardTag = tag;
            _shardings[index].Lock.EnterWriteLock();
            return this;
        }

        public IUnitOfWork<TKey, TValue, TShardKey> AddItem(TKey key, TValue value)
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            try
            {
                var _key = GetShardKey(value);
                var (_index, _tag) = Sharding(_key);
                if (_index != context.ShardIndex)
                    throw new ArgumentException("要创建的值不在当前分区上");

                _shardings[context.ShardIndex].Cache.Add(context.Key, value);
            }
            catch
            {
                _tls.Value = null;
                _shardings[context.ShardIndex].Lock.ExitWriteLock();
                throw;
            }
            return this;
        }

        public IUnitOfWork<TKey, TValue, TShardKey> GetItem(TKey key)
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            try
            {
                context.PreResult = _shardings[context.ShardIndex].Cache[key];
            }
            catch
            {
                _tls.Value = null;
                _shardings[context.ShardIndex].Lock.ExitWriteLock();
                throw;
            }
            return this;
        }

        public IUnitOfWork<TKey, TValue, TShardKey> DoWithResult(Action<TValue> action)
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            try
            {
                var pre_result = context.PreResult;
                action(pre_result);

                // 判定是否需要挪动分区
                var _shard = GetShardKey(pre_result);
                var (_new_index, _new_tag) = Sharding(_shard);
                if (_new_index != context.ShardIndex)
                {
                    // 从当前分区删除
                    _shardings[context.ShardIndex].Remove(context.Key);
                    // 加入到新分区
                    _shardings[_new_index].Add(context.Key, pre_result);
                }
            }
            catch
            {
                _tls.Value = null;
                _shardings[context.ShardIndex].Lock.ExitWriteLock();
                throw;
            }
            return this;
        }

        public IUnitOfWork<TKey, TValue, TShardKey> DoWithResult(Func<TValue, TValue> func)
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            try
            {
                var old_pre_result = context.PreResult;
                var new_pre_result = func(old_pre_result);
                context.PreResult = new_pre_result;

                // 判定是否需要挪动分区
                var _shard = GetShardKey(new_pre_result);
                var (_new_index, _new_tag) = Sharding(_shard);
                if (_new_index != context.ShardIndex)
                {
                    // 从当前分区删除
                    _shardings[context.ShardIndex].Remove(context.Key);
                    // 加入到新分区
                    _shardings[_new_index].Add(context.Key, new_pre_result);
                }
                else
                {
                    _shardings[context.ShardIndex].Cache[context.Key] = new_pre_result;
                }
            }
            catch
            {
                _tls.Value = null;
                _shardings[context.ShardIndex].Lock.ExitWriteLock();
                throw;
            }
            return this;
        }

        public IUnitOfWork<TKey, TValue, TShardKey> UpdateItem(TKey key, Action<TValue> update)
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            try
            {
                var val = _shardings[context.ShardIndex].Cache[key];
                update(val);

                // 判定是否需要挪动分区
                var _shard = GetShardKey(val);
                var (_new_index, _new_tag) = Sharding(_shard);
                if (_new_index != context.ShardIndex)
                {
                    // 从当前分区删除
                    _shardings[context.ShardIndex].Remove(key);
                    // 加入到新分区
                    _shardings[_new_index].Add(key, val);
                }
            }
            catch
            {
                _tls.Value = null;
                _shardings[context.ShardIndex].Lock.ExitWriteLock();
                throw;
            }
            return this;
        }

        public IUnitOfWork<TKey, TValue, TShardKey> UpdateItem(TKey key, Func<TValue, TValue> update)
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            try
            {
                var new_val = update(_shardings[context.ShardIndex].Cache[key]);

                // 判定是否需要挪动分区
                var _shard = GetShardKey(new_val);
                var (_new_index, _new_tag) = Sharding(_shard);
                if (_new_index != context.ShardIndex)
                {
                    // 从当前分区删除
                    _shardings[context.ShardIndex].Remove(key);
                    // 加入到新分区
                    _shardings[_new_index].Add(key, new_val);
                }
                else
                {
                    _shardings[context.ShardIndex].Cache[key] = new_val;
                }
            }
            catch
            {
                _tls.Value = null;
                _shardings[context.ShardIndex].Lock.ExitWriteLock();
                throw;
            }
            return this;
        }

        public IUnitOfWork<TKey, TValue, TShardKey> RemoveItem(TKey key)
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            try
            {
                _shardings[context.ShardIndex].Cache.Remove(key);
            }
            catch
            {
                _tls.Value = null;
                _shardings[context.ShardIndex].Lock.ExitWriteLock();
                throw;
            }
            return this;
        }

        public void Go()
        {
            if (!_tls.IsValueCreated)
                throw new InvalidOperationException("当前线程本地存储没有赋值");

            var context = _tls.Value;
            _tls.Value = null;
            _shardings[context.ShardIndex].Lock.ExitWriteLock();
        }
        #endregion
    }
}
