using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.Helpers.Core
{
    public interface ICacheService
    {
        T GetValueOrTimeOut<T>(string cacheItemKey);
        void FlushCacheItem(string cacheItemKey);
        void SetValue<T>(string cacheItemKey, T cacheObject);
    }
}
