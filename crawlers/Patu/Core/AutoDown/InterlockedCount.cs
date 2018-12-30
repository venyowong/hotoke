using System.Threading;

namespace Patu.AutoDown
{
    public class InterlockedCount
    {
        private int count;
        public int Count{get => this.count;}

        public int Increment()
        {
            return Interlocked.Increment(ref this.count);
        }
        
        public int Decrement()
        {
            return Interlocked.Decrement(ref this.count);
        }
    }
}