using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hotoke.MainSite.Models;
using Hotoke.SearchEngines;
using Hotoke.Common;
using NLog;
using Microsoft.Extensions.Caching.Memory;

namespace Hotoke.MainSite
{
    public static class SearchManager
    {
        private static readonly Logger _logger = LogManager.GetLogger("SearchManager", typeof(SearchManager));
        private static volatile MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly List<ISearchEngine> _engines = new List<ISearchEngine>();
        private static readonly Dictionary<string, float> _factorDic = new Dictionary<string, float>();

        static SearchManager()
        {
            _engines = ConfigurationManager.AppSettings["engines"].Split(',').Select<string, ISearchEngine>(engine => 
            {
                var strs = engine.Split(':');
                engine = strs[0];
                if(strs.Length > 1 && float.TryParse(strs[1], out float factor))
                {
                    _factorDic.TryAdd(engine, factor);
                    _logger.Info($"Adding search engine {engine}, search factor: {_factorDic[engine]}");
                }
                else
                {
                    _factorDic.TryAdd(engine, 1f);
                    _logger.Info($"Adding search engine {engine}, search factor: 1.0");
                }

                switch(engine)
                {
                    case "google":
                        _logger.Info("Parsed as GoogleSearch");
                        return new GoogleSearch();
                    case "hotoke":
                        _logger.Info("Parsed as HotokeSearch");
                        return new HotokeSearch();
                    case "360":
                    case "baidu":
                    case "bing":
                    default:
                        _logger.Info("Parsed as GenericSearch");
                        return new GenericSearch(engine);
                }
            })
            .Where(engine => engine != null)
            .ToList();
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
            var spinLock = new SpinLock(false);

            Task.Run(() =>
            {
                Parallel.ForEach(_engines, engine =>
                {
                    SearchPerEngine(engine, keyword, english, result, spinLock);
                });

                result.Finished = true;
            });

            SpinWait.SpinUntil(() => result.Searched > 0 || result.Finished);

            SearchResultModel newResult = null;
            var gotlock = false;
            try
            {
                spinLock.Enter(ref gotlock);
                newResult = result.Copy();
            }
            catch(Exception e)
            {
                _logger.Error(e, "catched an exception when copying result.");
            }
            finally
            {
                spinLock.Exit();
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
            SearchResultModel result, SpinLock spinLock)
        {
            try
            {
                var searchResults = engine.Search(keyword, english);
                if(searchResults == null || searchResults.Count() <= 0)
                {
                    _logger.Warn($"The result is null or empty, when searching {keyword} by {engine.Name}");
                    return;
                }

                _logger.Info($"count of {engine.Name} results: {searchResults.Count()}");
                var gotlock = false;
                try
                {
                    spinLock.Enter(ref gotlock);
                    MergeResult(keyword, searchResults, result.Results, _factorDic[engine.Name]);
                }
                catch(Exception e)
                {
                    _logger.Error(e, "catched an exception when merging result.");
                }
                finally
                {
                    spinLock.Exit();
                }
                _logger.Info($"{engine.Name} results merged.");
                var count = result.Searched;
                result.Searched = Interlocked.Increment(ref count);
            }
            catch(Exception e)
            {
                _logger.Error(e, $"An exception occurred while searching for {keyword}");
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
                    var diff = StringUtility.LevenshteinDistance(keyword, result.Title) + 1;
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
                            if(r.Url.ContainsAny(Utility.BadUrls))
                            {
                                r.Url = result.Url;
                            }
                            else if(!Utility.BadUrls.Contains(result.Url) && !r.Uri.SameAs(result.Uri))
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
                        var diff = StringUtility.LevenshteinDistance(keyword, result.Title) + 1;
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