using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Hotoke.Common;
using log4net;
using Patu;
using Patu.Processor;

namespace Hotoke.PatuCrawler.Stackoverflow
{
    public class StackoverflowProcessor : IProcessor
    {
        private static ILog _logger = Utility.GetLogger(typeof(StackoverflowProcessor));

        public void Process(HtmlPage page, ICrawlContext context)
        {
            _logger.Info(page.Url);
            var questions = page.SelectNodes("//*[@id=\"questions\"]/div");
            if(questions == null || questions.Count <= 0)
            {
                _logger.Warn($"cannot get any question from {page.Url}");
            }

            questions.AsParallel().ForAll(question =>
            {
                try
                {
                    var data = new Dictionary<string, string>();
                    var questionId = question.SelectSingleNode("div[2]/h3/a")?.Attributes["href"].Value.Split('/')[2];
                    if(string.IsNullOrWhiteSpace(questionId))
                    {
                        return;
                    }

                    data.Add("url", $"https://stackoverflow.com/questions/{questionId}");
                    data.Add("desc", question.SelectSingleNode("div[2]/div[1]/text()")?.InnerText?.Trim());
                    data.Add("title", question.SelectSingleNode("div[2]/h3/a/text()")?.InnerText?.Trim());
                    var keywords = question.SelectNodes("div[2]/div[2]/a")?.Select(node => node?.InnerText);
                    if(keywords != null)
                    {
                        data.Add("keyword", string.Join(",", keywords));
                    }

                    HttpUtility.Post(ConfigurationManager.AppSettings["IndexHost"], data);
                }catch{}
            });
        }
    }
}