using System.Collections.Generic;
using System.Configuration;
using Patu;
using Patu.HttpClientFactories;

namespace Hotoke.PatuCrawler.Stackoverflow
{
    public class StackoverflowCrawler : Patu.PatuCrawler
    {
        public StackoverflowCrawler() : base(new PatuConfig
        {
            Interval = "1y",
            CrawlDeepth = 1
        }, new StackoverflowProcessor())
        {
            this.Config.Seeds = new List<string>();
            int.TryParse(ConfigurationManager.AppSettings["StackoverflowStart"], out int start);
            int.TryParse(ConfigurationManager.AppSettings["StackoverflowEnd"], out int end);
            for(int i = start; i <= end; i++)
            {
                this.Config.Seeds.Add($"https://stackoverflow.com/questions?page={i}&sort=frequent&pageSize=50");
            }
        }

        public override PatuCrawlTask GenerateTask()
        {
            return new PatuCrawlTask(this.Config, this.Config.Seeds, 
                this.processor, this.cancellation, new ContinuousProxyFactory(() =>
                {
                    return Utility.HttpGet(ConfigurationManager.AppSettings["ProxyPoolUrl"])?["http"]?.ToString();
                }));
        }
    }
}