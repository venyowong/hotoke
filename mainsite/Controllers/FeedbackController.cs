using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace Hotoke.MainSite.Controllers
{
    public class FeedbackController : Controller
    {
        private static readonly ConcurrentQueue<string> _keywords = new ConcurrentQueue<string>();
        
        public bool Keyword(string id)
        {
            _keywords.Enqueue(id);
            return true;
        }

        public string DequeueKeyword()
        {
            _keywords.TryDequeue(out string result);
            return result;
        }
    }
}