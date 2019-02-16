using System.Collections.Generic;

namespace CacheRepository
{
    public interface IRepository<TKey>
    {
        Dictionary<TKey, long> GloablHash { get; }
    }
}
