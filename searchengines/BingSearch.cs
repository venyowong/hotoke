using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Hotoke.Common;
using HtmlAgilityPack;

namespace Hotoke.SearchEngines
{
    public class BingSearch : ISearchEngine
    {
        public string Name{get => "bing";}
        
        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            var ensearch = english ? "&ensearch=1" : "";
            var html = HttpUtility.FetchHtml(new Uri(string.Format(
                ConfigurationManager.AppSettings["bingsearch"] ?? "https://cn.bing.com/search?q={0}{1}",
                System.Web.HttpUtility.UrlEncode(keyword), ensearch)));
            if(string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var results = doc.DocumentNode.SelectNodes("//ol[@id='b_results']/li[@class='b_algo']");
            if(results == null || results.Count == 0)
            {
                return null;
            }

            var searchResults = results.AsParallel().AsOrdered().Select(node =>
            {
                var result = new SearchResult
                {
                    Source = "bing"
                };

                var aNode = node.SelectSingleNode(".//h2/a");
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

                var desc = node.SelectSingleNode(".//div[@class='b_caption']/p");
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