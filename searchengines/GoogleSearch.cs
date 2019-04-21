using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using Hotoke.Common;
using HtmlAgilityPack;

namespace Hotoke.SearchEngines
{
    public class GoogleSearch : ISearchEngine
    {
        private static HttpClient HttpClient = null;

        public string Name{get => "google";}

        static GoogleSearch()
        {
            var address = ConfigurationManager.AppSettings["httpproxy"];

            if(!string.IsNullOrWhiteSpace(address))
            {
                // First create a proxy object
                var proxy = new WebProxy()
                {
                    Address = new Uri("address"),
                    UseDefaultCredentials = false,
                };

                // Now create a client handler which uses that proxy
                var httpClientHandler = new HttpClientHandler()
                {
                    Proxy = proxy,
                };

                // Finally, create the HTTP client object
                HttpClient = new HttpClient(handler: httpClientHandler, disposeHandler: true);
            }
            else
            {
                HttpClient = new HttpClient();
            }
        }
        
        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            var hl = english ? "us-en" : "zh-cn";
            var html = HttpUtility.Get(new Uri(string.Format(
                ConfigurationManager.AppSettings["googlesearch"] ?? "https://www.google.com.hk/search?hl={0}&q={1}",
                hl, System.Web.HttpUtility.UrlEncode(keyword))), HttpClient);
            if(string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var results = doc.DocumentNode.SelectNodes("//div[@id='ires']/ol/div");
            if(results == null || results.Count == 0)
            {
                return null;
            }

            var searchResults = results.AsParallel().AsOrdered().Select(node =>
            {
                var result = new SearchResult
                {
                    Source = "google"
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

                if(result.Url.StartsWith("/url?q="))
                {
                    var start = "/url?q=".Length;
                    var len = result.Url.IndexOf("&amp;") - start;
                    result.Url = result.Url.Substring(start, len);
                }
                else if(result.Url.StartsWith("/"))
                {
                    result.Url = $"https://www.google.com.hk{result.Url}";
                }
                result.Title = System.Web.HttpUtility.HtmlDecode(aNode.InnerText.Trim());

                var desc = node.SelectSingleNode(".//div[@class='s']/span[@class='st']");
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