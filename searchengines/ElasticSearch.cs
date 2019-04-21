using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Hotoke.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hotoke.SearchEngines
{
    public class ElasticSearch : ISearchEngine
    {
        private string host;

        public string Name => "hotoke";

        public ElasticSearch()
        {
            this.host = ConfigurationManager.AppSettings["eshost"];
        }

        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            var json = HttpUtility.Get(new Uri(
                $"{this.host}/_search?q={System.Web.HttpUtility.UrlEncode(keyword)}"));
            if(string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return null;
        }

        public IEnumerable<SearchResult> Search(string index, object query)
        {
            var json = HttpUtility.PostJson($"{this.host}/{index}/_search", query);
            return this.PacketResults(json);
        }

        private IEnumerable<SearchResult> PacketResults(string json)
        {
            var jobject = JsonConvert.DeserializeObject<JObject>(json);
            var maxScore = jobject?["hits"]?["max_score"].Value<float>() ?? 1.0f;
            var hits = jobject?["hits"]?["hits"];
            if(hits == null || !(hits is JArray hitArray) || hitArray.Count <= 0)
            {
                return null;
            }

            return hitArray.Select(hit => new SearchResult
            {
                Title = hit?["_source"]?["title"]?.ToString(),
                Url = hit?["_source"]?["url"]?.ToString(),
                Desc = hit?["_source"]?["description"]?.ToString(),
                Score = maxScore / (hit?["_score"]?.Value<float>() ?? 0.1f),
                Base = hitArray.Count,
                Source = this.Name
            });
        }
    }
}