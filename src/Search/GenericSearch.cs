using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Niolog;

namespace Hotoke.Search
{
    public class GenericSearch : ISearchEngine
    {
        private string baseUrl;
        private string nodesSelection;
        private string linkSelection;
        private string descSelection;
        private IConfiguration config;

        public string Name {get; private set;}

        public GenericSearch(IConfiguration config, string name)
        {
            this.config = config;
            this.Name = name;
            this.baseUrl = this.config[$"{this.Name}:url"];
            this.nodesSelection = this.config[$"{this.Name}:nodes"];
            this.linkSelection = this.config[$"{this.Name}:link"];
            this.descSelection = this.config[$"{this.Name}:desc"];
        }

        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            if(string.IsNullOrWhiteSpace(this.baseUrl))
            {
                return null;
            }

            var logger = NiologManager.CreateLogger();

            var lang = english ? "en" : "cn";
            var ensearch = english ? "1" : "0";
            var url = this.baseUrl.Replace("{keyword}", System.Web.HttpUtility.UrlEncode(keyword)).Replace("{lang}", lang).Replace("{ensearch}", ensearch);
            logger.Info()
                .Message($"{this.Name} url: {url}")
                .Write();
            var uri = new Uri(url);
            var html = HttpUtility.Get(uri);
            if(string.IsNullOrWhiteSpace(html))
            {
                logger.Warn()
                    .Message($"{this.Name} response is null or white space")
                    .Write();
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectAllNodes(this.nodesSelection);
            if(nodes == null || nodes.Count <= 0)
            {
                logger.Warn()
                    .Message($"cannot select nodes from {this.Name} response")
                    .SetTag("Problem", "bad query")
                    .SetTag("Url", url)
                    .Write();
                return null;
            }

            var searchResults = nodes.AsParallel().AsOrdered().Select(node =>
            {
                var result = new SearchResult
                {
                    Source = this.Name
                };

                var link = node.SelectFirstNode(this.linkSelection);
                if(link == null)
                {
                    return null;
                }

                var href = link.Attributes["href"]?.Value.Trim();
                if(string.IsNullOrWhiteSpace(href))
                {
                    return null;
                }
                if (HttpUtility.IsUrl(uri, ref href))
                {
                    result.Url = href;
                }
                else
                {
                    return null;
                }

                result.Title = System.Web.HttpUtility.HtmlDecode(link.InnerText.Trim());

                var desc = node.SelectFirstNode(this.descSelection);
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