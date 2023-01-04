using HtmlAgilityPack;
using Serilog;
using System.Web;
using V.Common.Extensions;
using V.Hotoke.Extensions;
using V.Hotoke.Models;
using V.Talog.Client;

namespace V.Hotoke.Engines
{
    public abstract class GenericEngine : ISearchEngine
    {
        private string nodesSelection;
        private string linkSelection;
        private string descSelection;
        private float weight;
        private int httpTimeout;
        private IHttpClientFactory clientFactory;
        private LogChannel logChannel;

        protected string baseUrl;

        public string Name { get; private set; }

        public GenericEngine(string name, IConfiguration config, IHttpClientFactory clientFactory, LogChannel logChannel)
        {
            this.Name = name;
            this.baseUrl = config[$"Engines:{name}:Url"];
            this.nodesSelection = config[$"Engines:{name}:Nodes"];
            this.linkSelection = config[$"Engines:{name}:Link"];
            this.descSelection = config[$"Engines:{name}:Desc"];
            float.TryParse(config[$"Engines:{name}:Weight"], out var weight);
            this.weight = weight;
            this.clientFactory = clientFactory;
            if (!int.TryParse(config["Engines:Timeout"], out int timeout))
            {
                timeout = 1000;
            }
            this.httpTimeout = timeout;
            this.logChannel = logChannel;
        }

        public async Task<List<SearchResult>> Search(string keyword, int pageIndex, bool english)
        {
            var url = this.GetSearchUrl(keyword, pageIndex, english);
            var uri = new Uri(url);

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Host", uri.Host);
            request.Headers.Add("User-Agent", $"V.Hotoke.Engines.{this.GetType().Name}");
            try
            {
                using var client = this.clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMilliseconds(this.httpTimeout);
                using var response = client.Send(request);
                if (!response.IsSuccessStatusCode)
                {
                    this.logChannel.Warn()
                        .Tag("engine", this.Name)
                        .Tag("status", response.StatusCode.ToString())
                        .Log($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {url}")
                        .Send();
                    Log.Warning($"{url} response is not success status code.");
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var nodes = doc.DocumentNode.SelectAllNodes(this.nodesSelection);
                if (nodes.IsNullOrEmpty())
                {
                    this.logChannel.Warn()
                        .Tag("engine", this.Name)
                        .Tag("problem", "nodes_empty")
                        .Log($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {url}")
                        .Send();
                    Log.Warning($"cannot select nodes from {this.Name} response, url: {url}");
                    return null;
                }

                var searchResults = nodes.Select(node =>
                {
                    var result = new SearchResult
                    {
                        Source = this.Name,
                        Sources = new List<string> { this.Name }
                    };

                    var link = node.SelectFirstNode(this.linkSelection);
                    if (link == null)
                    {
                        return null;
                    }

                    var href = link.Attributes["href"]?.Value.Trim();
                    if (string.IsNullOrWhiteSpace(href))
                    {
                        return null;
                    }

                    result.Url = href;
                    if (result.Url.StartsWith('/'))
                    {
                        result.Url = $"{uri.Scheme}://{uri.Host}{result.Url}";
                    }
                    result.Title = HttpUtility.HtmlDecode(link.InnerText.Trim());
                    var desc = node.SelectFirstNode(this.descSelection);
                    result.Desc = HttpUtility.HtmlDecode(desc?.InnerText.Trim());
                    return result;
                })
                .Where(result => result != null).ToList();
                for (int i = 0; i < searchResults.Count; i++)
                {
                    searchResults[i].Score = 1.0F / (i + 1 + pageIndex * 10) * this.weight;
                }

                return searchResults;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("timeout"))
                {
                    this.logChannel.Warn()
                        .Tag("engine", this.Name)
                        .Tag("problem", "timeout")
                        .Log($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {url}")
                        .Send();
                }
                else
                {
                    this.logChannel.Warn()
                        .Tag("engine", this.Name)
                        .Tag("problem", "exception")
                        .Log($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {url} {ex}")
                        .Send();
                }
                Log.Warning(ex, $"发生异常, url: {url}");
                return null;
            }
        }

        public abstract string GetSearchUrl(string keyword, int pageIndex, bool english);
    }
}
