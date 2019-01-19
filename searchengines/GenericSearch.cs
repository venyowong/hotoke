using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Hotoke.Common;
using HtmlAgilityPack;

namespace Hotoke.SearchEngines
{
    public class GenericSearch : ISearchEngine
    {
        private string baseUrl;
        private string nodesSelection;
        private string linkSelection;
        private string descSelection;

        public string Name {get; private set;}

        public GenericSearch(string name)
        {
            this.Name = name;
            this.baseUrl = ConfigurationManager.AppSettings[$"{this.Name}.url"];
            this.nodesSelection = ConfigurationManager.AppSettings[$"{this.Name}.nodes"];
            this.linkSelection = ConfigurationManager.AppSettings[$"{this.Name}.link"];
            this.descSelection = ConfigurationManager.AppSettings[$"{this.Name}.desc"];
        }

        public IEnumerable<SearchResult> Search(string keyword, bool english = false)
        {
            if(string.IsNullOrWhiteSpace(this.baseUrl))
            {
                return null;
            }

            var lang = english ? "en" : "cn";
            var ensearch = english ? "1" : "0";
            var url = this.baseUrl.Replace("{keyword}", keyword).Replace("{lang}", lang).Replace("{ensearch}", ensearch);
            var html = HttpUtility.FetchHtml(new Uri(string.Format(
                url, System.Web.HttpUtility.UrlEncode(keyword), lang)));
            if(string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectAllNodes(this.nodesSelection);
            if(nodes == null || nodes.Count <= 0)
            {
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

                result.Url = link.Attributes["href"]?.Value.Trim();
                if(string.IsNullOrWhiteSpace(result.Url))
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