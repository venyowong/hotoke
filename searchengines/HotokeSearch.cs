using System;
using System.Collections.Generic;
using System.Configuration;
using Hotoke.Common;
using Newtonsoft.Json;

namespace Hotoke.SearchEngines
{
    public class HotokeSearch : ISearchEngine
    {
        public string Name => "hotoke";

        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            var json = HttpUtility.FetchHtml(new Uri(
                $"{ConfigurationManager.AppSettings["hotokesearch"]}/search?keyword={System.Web.HttpUtility.UrlEncode(keyword)}"));
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
                results[i].Base = (results.Count + 1) * 2;
                results[i].Score = (i + 1) * 2;
            }

            return results;
        }
    }
}