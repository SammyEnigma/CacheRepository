using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CacheRepository
{
    public class Shard<TKey, TValue, TShardKey>
    {
        private int _index;
        private string _tag;
        private ReaderWriterLockSlim _lock;
        private Dictionary<TKey, TValue> _cache;
        private IShardable<TKey, TValue, TShardKey> _repository;
        public ReaderWriterLockSlim Lock => this._lock;
        public Dictionary<TKey, TValue> Cache => this._cache;

        public Shard(int index, string tag, IShardable<TKey, TValue, TShardKey> repository)
        {
            _index = index;
            _tag = tag;
            _repository = repository;
            _lock = new ReaderWriterLockSlim();
            _cache = new Dictionary<TKey, TValue>();
        }

        public void Add(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Add(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public TValue Get(TKey key, bool deepClone = true)
        {
            TValue ret;
            _lock.EnterReadLock();
            try
            {
                if (typeof(TValue).IsValueType)
                {
                    ret = _cache[key];
                }
                else
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
                    if (typeof(TValue).IsValueType)
                    {
                        value = _cache[key];
                    }
                    else
                    {
                        if (deepClone)
                        {
                            value = CloneJson(_cache[key]);
                        }
                        else
                        {
                            value = _cache[key];
                        }
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
                    if (!typeof(TValue).IsValueType)
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
                        _cache[key] = ret;
                        if (!typeof(TValue).IsValueType)
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

        public bool TryUpdate(TKey key, Action<TValue> update)
        {
            bool ret = false;
            _lock.EnterWriteLock();
            TValue value;
            try
            {
                if (_cache.TryGetValue(key, out value))
                {
                    update(value);
                    // 判定是否需要挪动分区
                    var _shard = _repository.GetShardKey(value);
                    var (_new_index, _new_tag) = _repository.GetShardingRule()(_shard);
                    if (_new_index != this._index)
                    {
                        // 从当前分区删除
                        _cache.Remove(key);
                        // 加入到新分区
                        _repository[_new_index].Add(key, value);
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

        public bool TryUpdate(TKey key, Func<TValue, TValue> update)
        {
            bool ret = false;
            _lock.EnterWriteLock();
            TValue value;
            try
            {
                if (_cache.TryGetValue(key, out value))
                {
                    var new_val = update(value);
                    // 判定是否需要挪动分区
                    var _shard = _repository.GetShardKey(new_val);
                    var (_new_index, _new_tag) = _repository.GetShardingRule()(_shard);
                    if (_new_index != this._index)
                    {
                        // 从当前分区删除
                        _cache.Remove(key);
                        // 加入到新分区
                        _repository[_new_index].Add(key, new_val);
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

        public bool Remove(TKey key)
        {
            bool ret = false;
            _lock.EnterWriteLock();
            try
            {
                ret = _cache.Remove(key);
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

        private void TransferToShard(int src_index, int des_index)
        {

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
