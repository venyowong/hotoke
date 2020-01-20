using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hotoke.Models;
using Microsoft.Extensions.Configuration;
using Niolog;

namespace Hotoke.Search
{
    public abstract class BaseMetaSearcher
    {
        private IConfiguration config;

        private MetaSearcherConfig searcherConfig;

        private string[] badUrls;

        public BaseMetaSearcher(IConfiguration config, MetaSearcherConfig searcherConfig)
        {
            this.config = config;
            this.searcherConfig = searcherConfig;
            badUrls = this.config["badurls"]?.Split(';');
        }

        public IEnumerable<ISearchEngine> GetEngineList()
        {
            var enginesConfig = this.config["engines"];
            if (string.IsNullOrWhiteSpace(enginesConfig))
            {
                return new List<ISearchEngine>();
            }

            var engines = enginesConfig.Split(',');
            if (engines == null || engines.Length <= 0)
            {
                return new List<ISearchEngine>();
            }

            return engines.Select(engine =>
            {
                var strs = engine.Split(':');
                engine = strs[0];
                var searchEngine = this.searcherConfig?.GetSearchEngine(engine);
                if (searchEngine == null)
                {
                    searchEngine = new GenericSearch(this.config, engine);
                }

                if(strs.Length > 1 && float.TryParse(strs[1], out float weight))
                {
                    searchEngine.Weight = weight;
                }
                else
                {
                    searchEngine.Weight = 1.0f;
                }
                return searchEngine;
            });
        }

        protected void SearchOnEngine(ISearchEngine engine, string keyword, bool english, SearchResultModel result)
        {
            if (engine == null || string.IsNullOrWhiteSpace(keyword))
            {
                return;
            }

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
                        MergeResult(keyword, searchResults, result.Results, engine.Weight);
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

        protected void MergeResult(string keyword, IEnumerable<SearchResult> searchResults, 
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

        public abstract SearchResultModel GetSearchResult(string requestId, string keyword, bool async = true);
    }
}