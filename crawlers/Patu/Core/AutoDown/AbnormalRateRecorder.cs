using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Patu.AutoDown
{
    /// <summary>
    /// 异常速率记录器-记录爬虫过程中发生异常事件的速率
    /// </summary>
    public class AbnormalRateRecorder : IDisposable
    {
        private ConcurrentDictionary<string, InterlockedCount> rateDic = 
            new ConcurrentDictionary<string, InterlockedCount>();
        private bool disposed = false;

        public int Rate
        {
            get
            {
                var key = this.FormatDateTime(DateTime.Now);
                this.rateDic.TryGetValue(key, out InterlockedCount count);
                return count?.Count ?? 0;
            }
        }

        public AbnormalRateRecorder()
        {
            Task.Run(() =>
            {
                while(!this.disposed)
                {
                    var threshold = this.FormatDateTime(DateTime.Now.AddMinutes(-1));
                    foreach(var key in rateDic?.Keys.Where(k => k.CompareTo(threshold) < 0))
                    {
                        this.rateDic?.TryRemove(key, out InterlockedCount value);
                    }
                }
            });
        }

        public int Increment()
        {
            var key = this.FormatDateTime(DateTime.Now);
            this.rateDic.TryGetValue(key, out InterlockedCount count);
            if(count == null)
            {
                count = new InterlockedCount();
                this.rateDic.TryAdd(key, count);
            }

            return count.Increment();
        }

        public void Dispose()
        {
            this.disposed = true;
            this.rateDic = null;
        }

        private string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}