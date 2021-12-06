using Newtonsoft.Json;
using StackExchange.Redis;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers.Core;
using System;

namespace Sussex.Lhcra.Roci.Viewer.UI.Helpers
{
    public class CacheService : ICacheService
    {
        private readonly ConnectionMultiplexer cacheContext;
        private readonly IDatabase cacheInstance;

        public CacheService(string cacheConnectionString)
        {
            cacheContext = ConnectionMultiplexer.Connect(cacheConnectionString);
            cacheInstance = cacheContext.GetDatabase();
        }

        public void SetValue<T>(string cacheItemKey, T cacheObject)
        {
            var serializedCacheValue = JsonConvert.SerializeObject(cacheObject);
            cacheInstance.StringSet(cacheItemKey, serializedCacheValue);
        }



        public T GetValueOrTimeOut<T>(string cacheItemKey)
        {
            var timeoutValue = 6;

            string returnCacheValue = null;

            var startTime = DateTime.Now.ToUniversalTime();
            var finishTime = DateTime.Now.AddMilliseconds(timeoutValue).ToUniversalTime();

            try
            {
                while (startTime < finishTime)
                {
                    if (cacheInstance.StringGet(cacheItemKey).IsNull)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        returnCacheValue = cacheInstance.StringGet(cacheItemKey);
                        break;
                    }

                    startTime = DateTime.Now.ToUniversalTime();
                }

                // ToDo : Go over this with Bruno
                if (!string.IsNullOrEmpty(returnCacheValue))
                {
                    var convertedObject = JsonConvert.DeserializeObject<T>(returnCacheValue);

                    return convertedObject;
                }

                return default(T);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
        public void FlushCacheItem(string cacheItemKey)
        {
            cacheInstance.KeyDelete(cacheItemKey);
        }
    }
}
