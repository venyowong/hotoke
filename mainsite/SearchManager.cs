using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hotoke.MainSite.Models;
using Hotoke.Common;
using NLog;
using Microsoft.Extensions.Caching.Memory;
using Hotoke.Common.Entities;

namespace Hotoke.MainSite
{
    public class SearchManager
    {
        private static readonly Logger _logger = LogManager.GetLogger("SearchManager", typeof(SearchManager));
        private static volatile MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly IEnumerable<ISearchEngine> engines = null;
        private static readonly Dictionary<string, float> _factorDic = new Dictionary<string, float>();

        static SearchManager()
        {
            foreach(var engine in ConfigurationManager.AppSettings["factors"].Split(','))
            {
                var strs = engine.Split(':');
                var name = strs[0];
                if(strs.Length > 1 && float.TryParse(strs[1], out float factor))
                {
                    _factorDic.TryAdd(name, factor);
                    _logger.Info($"Adding search engine {name} factor: {_factorDic[name]}");
                }
                else
                {
                    _factorDic.TryAdd(name, 1f);
                    _logger.Info($"Adding search engine {name} factor: 1.0");
                }
            }
        }

        public SearchManager(IEnumerable<ISearchEngine> engines)
        {
            this.engines = engines;
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
            _cache.Set(requestId, result, new TimeSpan(0, 1, 0));
            var english = !keyword.HasOtherLetter();
            result.Results = new List<SearchResult>();

            Task.Run(() =>
            {
                Parallel.ForEach(engines, engine =>
                {
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
                _logger.Error(e, "catched an exception when copying result.");
            }
            return newResult;
        }

        public SearchResultModel GetSearchResult(string requestId, string keyword)
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

        public SearchResultModel GetSearchResultById(string requestId)
        {
            return GetSearchResult(requestId, string.Empty);
        }

        private void SearchPerEngine(ISearchEngine engine, string keyword, bool english, 
            SearchResultModel result)
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
                try
                {
                    lock(result)
                    {
                        MergeResult(keyword, searchResults, result.Results, _factorDic[engine.Name]);
                    }
                }
                catch(Exception e)
                {
                    _logger.Error(e, "catched an exception when merging result.");
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