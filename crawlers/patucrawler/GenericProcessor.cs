using System.Collections.Generic;
using System.Configuration;
using Hotoke.Common;
using log4net;
using Patu;
using Patu.Processor;
using StanSoft;

namespace Hotoke.PatuCrawler
{
    public class GenericProcessor : IProcessor
    {
        private static ILog _logger = Utility.GetLogger(typeof(GenericProcessor));
        private static string _indexHost = ConfigurationManager.AppSettings["IndexHost"];

        public void Process(HtmlPage page, ICrawlContext context)
        {
            _logger.Info(page.Url);
            var head = page.SelectSingleNode("//head");
            var title = head?.SelectSingleNode("//title")?.InnerText;
            if(string.IsNullOrWhiteSpace(title))
            {
                _logger.Warn($"cannot get title from {page.Url}");
                return;
            }
            var keywords = head?.SelectSingleNode("//meta[@name='keywords']")?.Attributes["Content"]?.Value;
            var description = head?.SelectSingleNode("//meta[@name='description']")?.Attributes["Content"]?.Value;
            var article = Html2Article.GetArticle(page.Content);
            var content = article.Content;

            var links = page.SelectNodes("//a");
            if(links != null)
            {
                foreach(var link in links)
                {
                    var url = link.Attributes["href"]?.Value;
                    if(Utility.IsUrl(page.Uri, ref url))
                    {
                        context.AddSeeds(url);
                    }
                }
            }

            var data = new Dictionary<string, string>();
            data.Add("url", page.Url);
            data.Add("title", title);
            if(!string.IsNullOrWhiteSpace(description))
            {
                data.Add("desc", description);
            }
            if(!string.IsNullOrWhiteSpace(keywords))
            {
                data.Add("keyword", keywords);
            }
            if(!string.IsNullOrWhiteSpace(content))
            {
                data.Add("content", content);
            }
            HttpUtility.Post(_indexHost, data);
        }
    }
}