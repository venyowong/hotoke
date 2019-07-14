using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Niolog;
using Niolog.Interfaces;

namespace Hotoke.MainSite.Controllers
{
    public class FeedbackController : Controller
    {
        private static readonly ConcurrentQueue<string> _keywords = new ConcurrentQueue<string>();

        private INiologger logger;

        public FeedbackController()
        {
            this.logger = NiologManager.CreateLogger();
        }
        
        public bool Keyword(string id)
        {
            this.logger.Trace()
                .SetTag("Feedback", "keyword")
                .Message(id)
                .Write();
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