using log4net;

namespace Patu.Processor
{
    public class PatuProcessor : IProcessor
    {
        private static ILog _logger = Utility.GetLogger(typeof(PatuProcessor));

        public void Process(HtmlPage page, ICrawlContext context)
        {
            _logger.Info(page.Url);
            var head = page.SelectSingleNode("//head");
            var title = head?.SelectSingleNode("//title");
            if(title != null)
            {
                _logger.Info($"title: {title.InnerText}");
            }
            var keywords = head?.SelectSingleNode("//meta[@name='keywords']");
            if(keywords != null)
            {
                _logger.Info($"keywords: {keywords.Attributes["Content"]?.Value}");
            }
            var description = head?.SelectSingleNode("//meta[@name='description']");
            if(description != null)
            {
                _logger.Info($"description: {description.Attributes["Content"]?.Value}");
            }

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
        }
    }
}