using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Hotoke.Common;
using HtmlAgilityPack;

namespace Hotoke.SearchEngines
{
    /// <summary>
    /// 360 搜索
    /// </summary>
    public class SoSearch : ISearchEngine
    {
        public string Name => "360";

        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            var html = HttpUtility.FetchHtml(new Uri(string.Format(
                ConfigurationManager.AppSettings["sosearch"] ?? "https://www.so.com/s?q={0}", 
                System.Web.HttpUtility.UrlEncode(keyword))));
            if(string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var results = doc.DocumentNode.SelectNodes("//ul[@class='result']/li[@class='res-list']");
            if(results == null || results.Count == 0)
            {
                return null;
            }

            var searchResults = results.AsParallel().AsOrdered().Select(node =>
            {
                var result = new SearchResult
                {
                    Source = "360"
                };

                var aNode = node.SelectSingleNode(".//h3/a");
                if(aNode == null)
                {
                    return null;
                }

                result.Url = aNode.Attributes["href"]?.Value.Trim();
                if(string.IsNullOrWhiteSpace(result.Url))
                {
                    return null;
                }

                result.Title = System.Web.HttpUtility.HtmlDecode(aNode.InnerText.Trim());

                var desc = node.SelectSingleNode(".//div/div/p");
                if(desc == null)
                {
                    desc = node.SelectSingleNode(".//p[@class='res-desc']");
                }
                result.Desc = System.Web.HttpUtility.HtmlDecode(desc?.InnerText.Trim());

                return result;
            })
            .Where(result => result != null).ToList();
            var count = searchResults.Count();
            for(int i = 0; i < count; i++)
            {
                searchResults[i].Score = i + 1;
                searchResults[i].Base = count + 1;
            }

            return searchResults;
        }
    }
}