using System;
using System.Threading;
using Hotoke.PatuCrawler.Stackoverflow;

namespace Hotoke.PatuCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            using(var crawler = new StackoverflowCrawler())
            {
                crawler.Start();
                Thread.Sleep(1000);
                SpinWait.SpinUntil(() => !crawler.Running);
            }
        }
    }
}
