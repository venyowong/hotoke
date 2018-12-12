using System;
using System.Collections.Generic;
using System.Linq;
using Hotoke.Common;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StanSoft;

namespace Hotoke.MainSite.Controllers
{
    public class BehaviorController : Controller
    {
        private readonly ILogger<BehaviorController> logger;
        private readonly IConfiguration configuration;

        public BehaviorController(ILogger<BehaviorController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpPost]
        public void Browse(string url)
        {
            if(string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            var data = new Dictionary<string, string>();
            data.Add("url", url);
            var html = HttpUtility.FetchHtml(new Uri(url));
            var article = Html2Article.GetArticle(html);
            data.Add("content", article.Content);
            var document = new HtmlDocument();
            document.LoadHtml(html);
            var head = document.DocumentNode.SelectSingleNode("//head");
            var title = head?.SelectSingleNode("//title");
            if(title == null)
            {
                return;
            }

            data.Add("title", title.InnerText.Trim());
            var keywords = head?.SelectSingleNode("//meta[@name='keywords']");
            if(keywords != null)
            {
                data.Add("keywords", string.Join(',', keywords.Attributes["Content"]?.Value
                    .Split(',', ';', ' ')
                    .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                    .Select(keyword => keyword.Trim())));
            }
            var description = head?.SelectSingleNode("//meta[@name='description']");
            if(description != null)
            {
                data.Add("desc", description.Attributes["Content"]?.Value.Trim());
            }

            HttpUtility.Post($"{this.configuration["SearchHost"]}/index", data);
        }
    }
}