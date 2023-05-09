using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OsuMissAnalyzer.Server.Logging;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server
{
    public class ResponseCache : IDisposable
    {
        private readonly MemoryCache cachedMisses;
        private readonly IDataLogger dLog;
        private readonly ServerOptions options;

        public ResponseCache(IOptions<ServerOptions> options, IDataLogger dLog)
        {
            this.cachedMisses = new MemoryCache(new MemoryCacheOptions { TrackStatistics = true });
            this.dLog = dLog;
            this.options = options.Value;
        }

        public async Task<Response> GetOrCreateResponse(ulong id, Response response)
        {
            return await cachedMisses.GetOrCreateAsync(id, CreateResponse(response));
        }

        public async Task UpdateResponse(object e, Response response, int index)
        {
            dLog.Log(DataPoint.MessageEdited);
            var id = await response.UpdateResponse(e, index);
            if (id.HasValue) await GetOrCreateResponse(id.Value, response);
        }

        public bool TryGetResponse(ulong id, out Response response)
        {
            return cachedMisses.TryGetValue(id, out response);
        }

        private Func<ICacheEntry, Task<Response>> CreateResponse(Response response)
        {
            dLog.Log(DataPoint.MessageCreated);
            dLog.LogAbsolute(DataPoint.CachedMessages, (int)cachedMisses.GetCurrentStatistics().CurrentEntryCount);
            return entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromSeconds(options.MessageExpiration);
                entry.RegisterPostEvictionCallback(OnEvict);
                return Task.FromResult(response);
            };
        }

        public async void OnEvict(object key, object value, EvictionReason reason, object state)
        {
            await (value as Response).OnExpired();
        }

        public void Dispose()
        {
            cachedMisses.Clear();
            cachedMisses.Dispose();
        }
    }
}