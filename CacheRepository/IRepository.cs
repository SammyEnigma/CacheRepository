namespace CacheRepository
{
    public interface IRepository<TKey>
    {
        IWriteBack Syncer { get; }
    }
}
