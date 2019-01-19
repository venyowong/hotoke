using System.Configuration;
using Patu;
using Patu.Config;
using Patu.HttpClientFactories;
using Patu.Processor;

namespace Hotoke.PatuCrawler
{
    public class GenericCrawler : Patu.PatuCrawler
    {
        public GenericCrawler(IProcessor processor = null) : base(processor){}

        public GenericCrawler(PatuConfig config, IProcessor processor = null) : base(config, processor){}

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