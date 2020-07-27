using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hotoke.Core.Engines;
using Hotoke.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Hotoke.Core.Searchers
{
    public class ParallelSearcher : BaseMetaSearcher
    {
        private volatile MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        private readonly IEnumerable<ISearchEngine> engines = null;

        private static readonly TimeSpan _expirationTime = new TimeSpan(0, 1, 0);
        
        public ParallelSearcher(IConfiguration config, MetaSearcherConfig searcherConfig) : base(config, searcherConfig)
        {
            this.engines = base.GetEngineList();
        }

        public override SearchResultModel GetSearchResult(string requestId, string keyword, bool async = true)
        {
            if(string.IsNullOrWhiteSpace(requestId))
            {
                return GetSearchResult(keyword, async);
            }

            cache.TryGetValue(requestId, out SearchResultModel result);
            if(result == null && !string.IsNullOrWhiteSpace(keyword))
            {
                return GetSearchResult(keyword, async);
            }
            
            return result;
        }

        public SearchResultModel GetSearchResult(string keyword, bool async = true)
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

            if (async)
            {
                return this.GetSearchResultAsync(keyword, english, result);
            }
            else
            {
                return this.GetSearchResultSync(keyword, english, result);
            }
        }

        public SearchResultModel GetSearchResultAsync(string keyword, bool english, SearchResultModel result)
        {
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

        public SearchResultModel GetSearchResultSync(string keyword, bool english, SearchResultModel result)
        {
            Parallel.ForEach(this.engines, engine =>
            {
                base.SearchOnEngine(engine, keyword, english, result);
            });

            result.Finished = true;
            return result;
        }
    }
}