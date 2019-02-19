using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CacheRepository
{
    public class Shard<TKey, TValue, TShardKey>
        where TValue : class
    {
        private int _index;
        private string _tag;
        private ReaderWriterLockSlim _lock;
        private Dictionary<TKey, TValue> _cache;
        private IShardable<TKey, TValue, TShardKey> _repository;
        public ReaderWriterLockSlim Lock { get => this._lock; }
        public Dictionary<TKey, TValue> Cache { get => this._cache; }

        public Shard(int index, string tag, IShardable<TKey, TValue, TShardKey> repository)
        {
            _index = index;
            _tag = tag;
            _repository = repository;
            _lock = new ReaderWriterLockSlim();
            _cache = new Dictionary<TKey, TValue>();
        }

        public bool Add(TKey key, TValue value, out int affected)
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Add(key, value);
                affected = 1;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return true;
        }

        public TValue Get(TKey key, bool deepClone = true)
        {
            TValue ret;
            _lock.EnterReadLock();
            try
            {
                TValue val = _cache[key];
                if (deepClone)
                {
                    ret = CloneJson(val);
                }
                else
                {
                    ret = val;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return ret;
        }

        public bool TryGet(TKey key, out TValue value, bool deepClone = true)
        {
            bool ret = false;
            _lock.EnterReadLock();
            try
            {
                if (_cache.ContainsKey(key))
                {
                    if (deepClone)
                    {
                        value = CloneJson(_cache[key]);
                    }
                    else
                    {
                        value = _cache[key];
                    }
                    ret = true;
                }
                else
                {
                    value = default(TValue);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return ret;
        }

        public TValue GetOrCreate(TKey key, Func<TValue> factory, bool deepClone = true)
        {
            TValue ret;
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_cache.ContainsKey(key))
                {
                    ret = _cache[key];
                    if (deepClone)
                    {
                        ret = CloneJson(ret);
                    }
                }
                else
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        ret = factory();
                        if (ret == null)
                        {
                            throw new ArgumentException("缓存值不能为null");
                        }

                        // 验证分片号一致
                        var _shardkey = _repository.GetShardKey(ret);
                        var (index, tag) = _repository.GetShardingRule()(_shardkey);
                        if (index != this._index)
                        {
                            throw new ArgumentException("创建的缓存对象分片与所在分片不一致");
                        }
                        _cache[key] = ret;
                        _repository.GloablHash.Add(key, ret.GetHashCode());
                        if (deepClone)
                        {
                            ret = CloneJson(ret);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }

            return ret;
        }

        /*
         * 说明：
         * 现有的判定value是否被更新的方式有一个问题：如果用户修改的字段以及要更新的值完全一致的话，
         * 那么hash计算出来的值将会是一样的，如此一来affected也就为0了。上层业务也就不能知晓更新是
         * 否成功；
         * 
         * 一个改进方式是，对value这个类型做手脚。核心思想是增加一个version隐藏字段，每当有更新发生
         * 的时候version会自动+1，这样的话就可以保证即使是更改的字段更改的值完全一样version也会发生
         * 变动，hash的结果也会不一样了；
         * 
         * 当中有两个小问题：
         *      1. 该version字段的读写应该要保证线程安全，原因在于虽然我们的更新发生在单个对象上，并
         *      且修改的当时整个shard是被锁住的，但是如果同时修改多个字段，我们也不清楚clr内部对字段
         *      赋值的动作会不会并行发生，出于安全考虑应该要进行线程同步处理；
         *      2. 考虑到内存的修改可能会很多，所以也许想用volatile来修饰一个long version。这其实没
         *      关系，即使用short也无妨，原因就在于我们的修改发生在单个对象上，这里仅需要保证version
         *      单调递增就可以了；
         *      
         * 具体的例子可以参考CacheRepository.Test项目中对Car类型的实现
         */
        public bool TryUpdate(TKey key, Action<TValue> update, out int affected)
        {
            bool ret = false;
            affected = 0;
            _lock.EnterWriteLock();
            TValue value;
            try
            {
                if (_cache.TryGetValue(key, out value))
                {
                    update(value);
                    var old_hash = _repository.GloablHash[key];
                    var new_hash = value.GetHashCode();
                    if (old_hash != new_hash)
                    {
                        affected = 1;
                    }

                    // 判定是否需要挪动分区
                    var _shard = _repository.GetShardKey(value);
                    var (_new_index, _new_tag) = _repository.GetShardingRule()(_shard);
                    if (_new_index != this._index)
                    {
                        // 从当前分区删除
                        _cache.Remove(key);
                        // 加入到新分区
                        _repository[_new_index].Add(key, value, out _);
                    }

                    ret = true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return ret;
        }

        public bool TryUpdate(TKey key, Func<TValue, TValue> update, out int affected)
        {
            bool ret = false;
            affected = 0;
            _lock.EnterWriteLock();
            TValue value;
            try
            {
                if (_cache.TryGetValue(key, out value))
                {
                    var new_val = update(value);
                    var old_hash = _repository.GloablHash[key];
                    var new_hash = new_val.GetHashCode();
                    if (old_hash != new_hash)
                    {
                        affected = 1;
                    }

                    // 判定是否需要挪动分区
                    var _shard = _repository.GetShardKey(new_val);
                    var (_new_index, _new_tag) = _repository.GetShardingRule()(_shard);
                    if (_new_index != this._index)
                    {
                        // 从当前分区删除
                        _cache.Remove(key);
                        // 加入到新分区
                        _repository[_new_index].Add(key, new_val, out _);
                    }
                    else
                    {
                        _cache[key] = new_val;
                    }

                    ret = true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return ret;
        }

        public bool Remove(TKey key, out int affected)
        {
            bool ret = false;
            _lock.EnterWriteLock();
            try
            {
                ret = _cache.Remove(key);
                affected = ret ? 1 : 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return ret;
        }

        public bool ContainsKey(TKey key)
        {
            bool ret = false;
            _lock.EnterReadLock();
            try
            {
                ret = _cache.ContainsKey(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return ret;
        }

        private TValue CloneJson(TValue source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(TValue);
            }

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            return JsonConvert.DeserializeObject<TValue>(JsonConvert.SerializeObject(source), deserializeSettings);
        }
    }
}
