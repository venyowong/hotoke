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
using Hotoke.MainSite.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace Hotoke.MainSite
{
    public static class SearchManager
    {
        private static readonly Logger _logger = LogManager.GetLogger("SearchManager", typeof(SearchManager));
        private static volatile MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly List<ISearchEngine> _engines = new List<ISearchEngine>();

        static SearchManager()
        {
            _engines = ConfigurationManager.AppSettings["Engines"].Split(',').Select(engine => 
            {
                switch(engine)
                {
                    case "bing":
                        return new BingSearch();
                    case "baidu":
                        return new BaiduSearch();
                    case "google":
                        return new GoogleSearch();
                    case "hotoke":
                        return new HotokeSearch();
                    case "360":
                        return new SoSearch();
                    default:
                        return default(ISearchEngine);
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

            Task.Run(() => StartSearchTask(keyword, result));

            SpinWait.SpinUntil(() => result.Searched > 0 || result.Finished);
            return result;
        }

        public static SearchResultModel GetSearchResult(string requestId, string keyword = "")
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

        public static void StartSearchTask(string keyword, SearchResultModel result)
        {
            var spinLock = new SpinLock();
            var english = !keyword.HasOtherLetter();
            result.Results = new List<SearchResult>();

            Parallel.ForEach(_engines, engine =>
            {
                try
                {
                    var searchResults = engine.Search(keyword, english);
                    if(searchResults == null)
                    {
                        return;
                    }

                    // 由于 hotoke 只爬取自己所感兴趣的内容，所以对于普遍的搜索来说
                    // 搜索效果较为无法令人满意，因此 hotoke 不能第一个展示出来
                    // 这样也能让其他第一个结束的搜索引擎在 merge 的时候，不用去重
                    // 可以快速地响应
                    if(result.Results.Count == 0 && engine.Name == "hotoke")
                    {
                        SpinWait.SpinUntil(() => result.Results.Count > 0);
                    }

                    var gotLock = false;
                    try
                    {
                        _logger.RecordInfo($"count of {engine.Name} results: {searchResults.Count()}");
                        spinLock.Enter(ref gotLock);
                        MergeResult(keyword, searchResults, result.Results);
                        _logger.RecordInfo($"{engine.Name} results merged.");
                        var count = result.Searched;
                        result.Searched = Interlocked.Increment(ref count);
                    }
                    finally
                    {
                        if(gotLock)
                        {
                            spinLock.Exit();
                        }
                    }
                }
                catch(Exception e)
                {
                    _logger.RecordError(e, $"An exception occurred while searching for {keyword}");
                }
            });

            result.Finished = true;
        }

        public static void MergeResult(string keyword, IEnumerable<SearchResult> searchResults, List<SearchResult> results)
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
                        if(r.Uri.SameAs(result.Uri) || r.Title == result.Title || r.Title.SimilarWith(result.Title))
                        {
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