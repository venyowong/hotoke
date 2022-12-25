using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using V.Common.Extensions;
using V.Hotoke.Models;
using V.SwitchableCache;

namespace V.Hotoke.Engines
{
    public class MetaSearcher
    {
        private IEnumerable<ISearchEngine> engines;
        private ICacheService cache;

        public MetaSearcher(IEnumerable<ISearchEngine> engines, ICacheService cache)
        {
            this.engines = engines;
            this.cache = cache;
        }

        public async Task<PagedResult<SearchResult>> Search(string keyword, int pageIndex)
        {
            var key = $"V:Hotoke:Engines:MetaSearcher:Search:{keyword}";
            var json = this.cache.StringGet(key);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var list = json.ToObj<List<SearchResult>>();
                if (list == null)
                {
                    return new PagedResult<SearchResult> { Code = -1, Msg = "json 反序列化失败" };
                }

                return new PagedResult<SearchResult>
                {
                    Code = 0,
                    Total = list.Count,
                    Items = list.Skip(pageIndex * 10).Take(10).ToList()
                };
            }

            var english = this.IsEnglishKeyword(keyword);
            var results = new ConcurrentBag<SearchResult>();
            await Parallel.ForEachAsync(this.engines, async (engine, token) =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                var list = await engine.Search(keyword, 0, english);
                if (list != null)
                {
                    list.ForEach(x => results.Add(x));
                }
            });

            var searchResults = this.MergeResults(results);
            this.cache.StringSet(key, searchResults.ToJson(), TimeSpan.FromMinutes(1));
            return new PagedResult<SearchResult>
            {
                Code = 0,
                Total = searchResults.Count,
                Items = searchResults.Skip(pageIndex * 10).Take(10).ToList()
            };
        }

        private bool IsEnglishKeyword(string keyword) => !keyword.Any(c => char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter);

        private List<SearchResult> MergeResults(IEnumerable<SearchResult> results)
        {
            var result = new List<SearchResult>();
            foreach (var item in results)
            {
                var added = false;

                foreach (var x in result)
                {
                    if (item.SimilarWith(x))
                    {
                        added= true;
                        if (!x.Sources.Contains(item.Source))
                        {
                            x.Sources.Add(item.Source);
                        }
                        if (x.Score < item.Score)
                        {
                            x.Score = x.Score * (1- 1.0f / (x.Sources.Count + 1));
                        }
                        else
                        {
                            x.Score = item.Score * (1 - 1.0f / (x.Sources.Count + 1));
                        }
                        x.Score = (x.Score * x.Sources.Count + item.Score) / (x.Sources.Count + 1);
                        if (item.Url.Length < x.Url.Length)
                        {
                            x.UpdateUrl(item.Url);
                        }
                        if (string.IsNullOrWhiteSpace(x.Desc))
                        {
                            x.Title = item.Title;
                            x.Desc= item.Desc;
                        }
                        break;
                    }
                }

                if (!added)
                {
                    result.Add(item);
                }
            }
            return result.OrderBy(x => x.Score).ToList();
        }
    }
}
