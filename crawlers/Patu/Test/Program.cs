using System;
using System.Threading;
using Patu;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // using(var crawler = new PatuCrawler())
            // {
            //     crawler.Start();
            //     //Console.Read();
            //     Thread.Sleep(2000);
            //     crawler.Restart();
            //     Thread.Sleep(5000);
            //     //Console.Read();
            //     crawler.Stop();
            // }
            var bloom = new BloomFilter<string>(100, 5);
            bloom.Add("sdflaskjdfa");
            bloom.Add("fasjdjojw");
            bloom.Save(AppDomain.CurrentDomain.BaseDirectory, "test");
            var bloom2 = BloomFilter<string>.LoadFromFile(AppDomain.CurrentDomain.BaseDirectory, "test");
            var bytes = new byte[13];
            bloom.BitArray.Xor(bloom2.BitArray).CopyTo(bytes, 0);
            Console.WriteLine(string.Join(' ', bytes));
        }
    }
}
