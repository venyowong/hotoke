using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Hotoke.Common;
using HtmlAgilityPack;

namespace Hotoke.SearchEngines
{
    public class BaiduSearch : ISearchEngine
    {
        public string Name{get => "baidu";}
        
        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            var lang = english ? "en" : "cn";
            var html = HttpUtility.FetchHtml(new Uri(string.Format(
                ConfigurationManager.AppSettings["baidusearch"] ?? "http://www.baidu.com/s?wd={0}&ie=utf-8&rqlang={1}", 
                System.Web.HttpUtility.UrlEncode(keyword), lang)));
            if(string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var results = doc.DocumentNode.SelectNodes("//div[@id='content_left']/div[@class='result-op c-container']");
            results = results.AddRange(doc.DocumentNode.SelectNodes("//div[@id='content_left']/div[@class='result c-container ']"));
            if(results == null || results.Count == 0)
            {
                return null;
            }

            var searchResults = results.AsParallel().AsOrdered().Select(node =>
            {
                var result = new SearchResult
                {
                    Source = "baidu"
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

                var desc = node.SelectSingleNode(".//div[@class='c-abstract']");
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