namespace Sussex.Lhcra.Plexus.Viewer.UI.Helpers.Core
{
    public interface ICacheService
    {
        T GetValueOrTimeOut<T>(string cacheItemKey);
        void FlushCacheItem(string cacheItemKey);
        void SetValue<T>(string cacheItemKey, T cacheObject);
    }
}
