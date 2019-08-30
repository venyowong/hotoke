using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Niolog;
using Hotoke.Models;

namespace Hotoke.Search
{
    public static class SearchManager
    {
        private static volatile MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly IEnumerable<ISearchEngine> _engines = null;
        private static readonly Dictionary<string, float> _factorDic = new Dictionary<string, float>();
        private static string[] _badUrls;

        static SearchManager()
        {
            var logger = NiologManager.CreateLogger();

            try
            {
                _engines = ConfigurationManager.AppSettings["engines"].Split(',').Select<string, ISearchEngine>(engine => 
                {
                    var strs = engine.Split(':');
                    engine = strs[0];
                    if(strs.Length > 1 && float.TryParse(strs[1], out float factor))
                    {
                        _factorDic.TryAdd(engine, factor);
                        logger.Info()
                            .Message($"Adding search engine {engine}, search factor: {_factorDic[engine]}")
                            .Write();
                    }
                    else
                    {
                        _factorDic.TryAdd(engine, 1f);
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
                            return new GenericSearch(engine);
                    }
                });

                _badUrls = ConfigurationManager.AppSettings["badurls"].Split(';');
            }
            catch(Exception e)
            {
                logger.Error()
                    .Message("Failed to init SearchManager")
                    .Exception(e)
                    .Write();
                _badUrls = new string[0];
            }
        }

        public static SearchResultModel GetSearchResult(string keyword)
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
            _cache.Set(requestId, result, new TimeSpan(0, 1, 0));
            var english = !keyword.HasOtherLetter();
            result.Results = new List<SearchResult>();

            var logger = NiologManager.CreateLogger();
            Task.Run(() =>
            {
                NiologManager.Logger = logger;
                Parallel.ForEach(_engines, engine =>
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

        public static SearchResultModel GetSearchResult(string requestId, string keyword)
        {
            if(string.IsNullOrWhiteSpace(requestId))
            {
                return GetSearchResult(keyword);
            }

            _cache.TryGetValue(requestId, out SearchResultModel result);
            if(result == null && !string.IsNullOrWhiteSpace(keyword))
            {
                return GetSearchResult(keyword);
            }
            
            return result;
        }

        public static SearchResultModel GetSearchResultById(string requestId)
        {
            return GetSearchResult(requestId, string.Empty);
        }

        private static void SearchPerEngine(ISearchEngine engine, string keyword, bool english, 
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
                        MergeResult(keyword, searchResults, result.Results, _factorDic[engine.Name]);
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

        private static void MergeResult(string keyword, IEnumerable<SearchResult> searchResults, 
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
                            if(r.Url.ContainsAny(_badUrls))
                            {
                                r.Url = result.Url;
                            }
                            else if(!_badUrls.Contains(result.Url) && !r.Uri.SameAs(result.Uri))
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