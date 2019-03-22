namespace CacheRepository
{
    public interface IRepository<Tkey, TValue>
        where TValue : class, IEntity
    {
        IWriteBack<TValue> Syncer { get; }
    }
}
