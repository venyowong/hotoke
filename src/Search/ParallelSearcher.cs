using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hotoke.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Niolog;

namespace Hotoke.Search
{
    public class ParallelSearcher : BaseMetaSearcher
    {
        private volatile MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        private readonly IEnumerable<ISearchEngine> engines = null;
        
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

            var requestId = Guid.NewGuid().ToString();
            var result = new SearchResultModel
            {
                RequestId = requestId
            };
            cache.Set(requestId, result, new TimeSpan(0, 1, 0));
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
            var logger = NiologManager.CreateLogger();
            Task.Run(() =>
            {
                NiologManager.Logger = logger;
                Parallel.ForEach(this.engines, engine =>
                {
                    NiologManager.Logger = logger;
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
                logger.Error()
                    .Message("catched an exception when copying result.")
                    .Exception(e, true)
                    .Write();
            }
            return newResult;
        }

        public SearchResultModel GetSearchResultSync(string keyword, bool english, SearchResultModel result)
        {
            var logger = NiologManager.CreateLogger();
            Parallel.ForEach(this.engines, engine =>
            {
                NiologManager.Logger = logger;
                base.SearchOnEngine(engine, keyword, english, result);
            });

            result.Finished = true;
            return result;
        }
    }
}