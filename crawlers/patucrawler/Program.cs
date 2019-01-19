using System;
using System.Threading;
using Hotoke.PatuCrawler.Stackoverflow;

namespace Hotoke.PatuCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Patu.PatuCrawler crawler = null;
            if(args.Length > 0 && args[0] == "stackoverflow")
            {
                crawler = new StackoverflowCrawler();
            }
            if(crawler == null)
            {
                crawler = new GenericCrawler(new GenericProcessor());
            }
            using(crawler)
            {
                crawler.Start();
                Thread.Sleep(1000);
                SpinWait.SpinUntil(() => !crawler.Running);
            }
        }
    }
}
