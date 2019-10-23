using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hotoke.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Niolog;

namespace Hotoke.Search
{
    public class ComprehensiveSearcher
    {
        private IConfiguration config;
        private volatile MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        private readonly IEnumerable<ISearchEngine> engines = null;
        private readonly Dictionary<string, float> factorDic = new Dictionary<string, float>();
        private string[] badUrls;

        public ComprehensiveSearcher(IConfiguration config)
        {
            this.config = config;
            var logger = NiologManager.CreateLogger();

            try
            {
                engines = this.config["engines"].Split(',').Select<string, ISearchEngine>(engine => 
                {
                    var strs = engine.Split(':');
                    engine = strs[0];
                    if(strs.Length > 1 && float.TryParse(strs[1], out float factor))
                    {
                        factorDic.TryAdd(engine, factor);
                        logger.Info()
                            .Message($"Adding search engine {engine}, search factor: {factorDic[engine]}")
                            .Write();
                    }
                    else
                    {
                        factorDic.TryAdd(engine, 1f);
                        logger.Info()
                            .Message($"Adding search engine {engine}, search factor: 1.0")
                            .Write();
                    }

                    switch(engine)
                    {
                        case "360":
                        case "baidu":
                        case "bing":
                        default:
                            logger.Info()
                                .Message("Parsed as GenericSearch")
                                .Write();
                            return new GenericSearch(this.config, engine);
                    }
                });

                badUrls = this.config["badurls"].Split(';');
            }
            catch(Exception e)
            {
                logger.Error()
                    .Message("Failed to init SearchManager")
                    .Exception(e)
                    .Write();
                badUrls = new string[0];
            }
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
            Task.Run(() =>
            {
                NiologManager.Logger = logger;
                Parallel.ForEach(engines, engine =>
                {
                    NiologManager.Logger = logger;
                    SearchPerEngine(engine, keyword, english, result);
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

        public SearchResultModel GetSearchResult(string requestId, string keyword)
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

        public SearchResultModel GetSearchResultById(string requestId)
        {
            return GetSearchResult(requestId, string.Empty);
        }

        private void SearchPerEngine(ISearchEngine engine, string keyword, bool english, 
            SearchResultModel result)
        {
            var logger = NiologManager.CreateLogger();

            try
            {
                var searchResults = engine.Search(keyword, english);
                if(searchResults == null || searchResults.Count() <= 0)
                {
                    logger.Warn()
                        .Message($"The result is null or empty, when searching {keyword} by {engine.Name}")
                        .Write();
                    return;
                }

                logger.Info()
                    .Message($"count of {engine.Name} results: {searchResults.Count()}")
                    .Write();
                try
                {
                    lock(result)
                    {
                        MergeResult(keyword, searchResults, result.Results, factorDic[engine.Name]);
                    }
                }
                catch(Exception e)
                {
                    logger.Error()
                        .Message("catched an exception when merging result.")
                        .Exception(e, true)
                        .Write();
                }
                logger.Info()
                    .Message($"{engine.Name} results merged.")
                    .Write();
                var count = result.Searched;
                result.Searched = Interlocked.Increment(ref count);
            }
            catch(Exception e)
            {
                logger.Error()
                    .Message($"An exception occurred while searching for {keyword}")
                    .Exception(e, true)
                    .Write();
            }
        }

        private void MergeResult(string keyword, IEnumerable<SearchResult> searchResults, 
            List<SearchResult> results, float factor)
        {
            if(results.Count == 0)
            {
                results.AddRange(searchResults);
                results.ForEach(result => 
                {
                    result.Sources.Add(result.Source);
                    var max = Math.Max(keyword.Length, result.Title.Length) + 1;
                    var diff = Utility.LevenshteinDistance(keyword, result.Title) + 1;
                    result.Score *= (float)diff / (float)max;
                    result.Score *= factor;
                });
            }
            else
            {
                var newResults = new List<SearchResult>();
                foreach(var result in searchResults)
                {
                    bool same = false;
                    foreach(var r in results)
                    {
                        if(r.Title == result.Title || r.Title.SimilarWith(result.Title))
                        {
                            if(r.Url.ContainsAny(badUrls))
                            {
                                r.Url = result.Url;
                            }
                            else if(!badUrls.Contains(result.Url) && !r.Uri.SameAs(result.Uri))
                            {
                                continue;
                            }

                            same = true;

                            if(r.Score <= result.Score)
                            {
                                r.Score *= result.Score / result.Base;
                            }
                            else
                            {
                                r.Score = result.Score * (r.Score / r.Base);
                            }

                            if(!r.Sources.Contains(result.Source))
                            {
                                r.Sources.Add(result.Source);
                            }
                            break;
                        }
                    }

                    if(!same)
                    {
                        result.Sources.Add(result.Source);
                        var max = Math.Max(keyword.Length, result.Title.Length) + 1;
                        var diff = Utility.LevenshteinDistance(keyword, result.Title) + 1;
                        result.Score *= (float)diff / (float)max;
                        result.Score *= factor;
                        newResults.Add(result);
                    }
                }

                results.AddRange(newResults);
                newResults = results.OrderBy(result => result.Score).ToList();
                results.Clear();
                results.AddRange(newResults);
            }
        }
    }
}