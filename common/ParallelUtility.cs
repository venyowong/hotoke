using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hotoke.Common
{
    public static class ParallelUtility
    {
        public static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            if(source == null || body == null)
            {
                return;
            }

            var count = new InterlockedCount();

            foreach(var item in source)
            {
                TaskUtility.Run(() =>
                {
                    body.Invoke(item);
                    count.Increment();
                });
            }

            SpinWait.SpinUntil(() => source.Count() == count.Count);
        }
    }
}