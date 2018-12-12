using System;
using System.Linq;
using Hotoke.SearchEngines;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var time = DateTime.Now;
            var results = new BaiduSearch().Search("音乐播放器");
            Console.WriteLine(DateTime.Now - time);
            Console.WriteLine(results.Count());
            foreach(var result in results)
            {
                Console.WriteLine($"{result.Title} {result.Desc}");
            }
        }
    }
}
