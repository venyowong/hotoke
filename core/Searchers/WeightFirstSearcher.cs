using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Hotoke.Core.Engines;
using Hotoke.Core.Models;
using Serilog;

namespace Hotoke.Core.Searchers
{
    /// <summary>
    /// WeightFirstSearcher does not support sync search.
    /// </summary>
    public class WeightFirstSearcher : BaseMetaSearcher
    {
        private volatile MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        private readonly IEnumerable<ISearchEngine> engines = null;

        private readonly ISearchEngine firstEngine = null;

        private static readonly TimeSpan _expirationTime = new TimeSpan(0, 1, 0);

        public WeightFirstSearcher(IConfiguration config, MetaSearcherConfig searcherConfig) : base(config, searcherConfig)
        {
            this.engines = base.GetEngineList()
                .OrderBy(e => e.Weight);
            this.firstEngine = this.engines.FirstOrDefault();
            this.engines = this.engines.Where(e => e.Name != this.firstEngine?.Name);
        }

        public override SearchResultModel GetSearchResult(string requestId, string keyword, bool async = true)
        {
            if(string.IsNullOrWhiteSpace(requestId))
            {
                return GetSearchResult(keyword);
            }

            cache.TryGetValue(requestId, out SearchResultModel result);
            if(result == null && !string.IsNullOrWhiteSpace(keyword))
            {
                return GetSearchResult(keyword);
            }
            
            return result;
        }

        public SearchResultModel GetSearchResult(string keyword)
        {
            if(string.IsNullOrWhiteSpace(keyword))
            {
                return null;
            }
            if (this.cache.TryGetValue(keyword, out SearchResultModel result))
            {
                return result;
            }

            var requestId = Guid.NewGuid().ToString();
            result = new SearchResultModel
            {
                RequestId = requestId
            };
            cache.Set(requestId, result, _expirationTime);
            this.cache.Set(keyword, result, _expirationTime);
            var english = !keyword.HasOtherLetter();
            result.Results = new List<SearchResult>();

            base.SearchOnEngine(this.firstEngine, keyword, english, result);
            Task.Run(() =>
            {
                Parallel.ForEach(this.engines, engine =>
                {
                    base.SearchOnEngine(engine, keyword, english, result);
                });

                result.Finished = true;
            });

            SpinWait.SpinUntil(() => result.Searched > 0 || result.Finished);

            SearchResultModel newResult = null;
            try
            {
                lock(result)
                {
                    newResult = result.Copy();
                }
            }
            catch(Exception e)
            {
                Log.Error(e, "catched an exception when copying result.");
            }
            return newResult;
        }
    }
}