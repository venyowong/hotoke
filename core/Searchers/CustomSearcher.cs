using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hotoke.Core.Engines;
using Hotoke.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;

namespace Hotoke.Core.Searchers
{
    /// <summary>
    /// CustomSearcher does not support sync search.
    /// </summary>
    public class CustomSearcher : BaseMetaSearcher
    {
        private volatile MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        
        private readonly IEnumerable<ISearchEngine> firstEngines = null;

        private readonly IEnumerable<ISearchEngine> secondaryEngines = null;

        private static readonly TimeSpan _expirationTime = new TimeSpan(0, 1, 0);

        public CustomSearcher(IConfiguration config, MetaSearcherConfig searcherConfig, 
            IOptions<CustomSearcherConfig> customSearcherConfig) : base(config, searcherConfig)
        {
            this.secondaryEngines = base.GetEngineList();
            this.firstEngines = this.secondaryEngines.Where(e => IsAdvancedEngine(e.Name));
            this.secondaryEngines = this.secondaryEngines.Where(e => !IsAdvancedEngine(e.Name));

            bool IsAdvancedEngine(string name) => customSearcherConfig?.Value?.AdvancedList?.Contains(name) ?? false;
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

            Parallel.ForEach(this.firstEngines, engine =>
            {
                base.SearchOnEngine(engine, keyword, english, result);
            });
            Task.Run(() =>
            {
                Parallel.ForEach(this.secondaryEngines, engine =>
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