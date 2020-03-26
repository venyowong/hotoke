using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Niolog;
using Hotoke.Core.Engines;
using Hotoke.Core.Models;

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

            var requestId = Guid.NewGuid().ToString();
            var result = new SearchResultModel
            {
                RequestId = requestId
            };
            cache.Set(requestId, result, new TimeSpan(0, 1, 0));
            var english = !keyword.HasOtherLetter();
            result.Results = new List<SearchResult>();

            var logger = NiologManager.CreateLogger();
            base.SearchOnEngine(this.firstEngine, keyword, english, result);
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
    }
}