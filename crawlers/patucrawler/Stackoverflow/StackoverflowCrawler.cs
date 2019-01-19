using System.Collections.Generic;
using System.Configuration;
using Patu;
using Patu.Config;
using Patu.HttpClientFactories;

namespace Hotoke.PatuCrawler.Stackoverflow
{
    public class StackoverflowCrawler : GenericCrawler
    {
        public StackoverflowCrawler() : base(new StackoverflowProcessor())
        {
            this.Config.CrawlDeepth = 1;
            this.Config.Name = "stackoverflow";
            this.Config.Seeds = new List<string>();
            int.TryParse(ConfigurationManager.AppSettings["StackoverflowStart"], out int start);
            int.TryParse(ConfigurationManager.AppSettings["StackoverflowEnd"], out int end);
            for(int i = start; i <= end; i++)
            {
                this.Config.Seeds.Add($"https://stackoverflow.com/questions?page={i}&sort=frequent&pageSize=50");
            }
        }
    }
}