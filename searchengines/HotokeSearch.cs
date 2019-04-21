using System;
using System.Collections.Generic;
using System.Configuration;
using Hotoke.Common;
using Newtonsoft.Json;

namespace Hotoke.SearchEngines
{
    public class HotokeSearch : ISearchEngine
    {
        private string host;

        public string Name => "hotoke";

        public HotokeSearch()
        {
            this.host = ConfigurationManager.AppSettings["hotokesearch"];
        }

        public HotokeSearch(string host)
        {
            this.host = host;
        }

        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            var json = HttpUtility.Get(new Uri(
                $"{this.host}/search?keyword={System.Web.HttpUtility.UrlEncode(keyword)}"));
            if(string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var results = JsonConvert.DeserializeObject<List<SearchResult>>(json);
            if(results == null)
            {
                return null;
            }

            for(int i = 0; i < results.Count; i++)
            {
                results[i].Source = "hotoke";
                results[i].Base = results.Count + 1;
                results[i].Score = i + 1;
            }

            return results;
        }
    }
}