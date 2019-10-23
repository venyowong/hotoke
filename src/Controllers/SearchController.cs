using Hotoke.Models;
using Hotoke.Search;
using Microsoft.AspNetCore.Mvc;

namespace Hotoke.Controllers
{
    public class SearchController : Controller
    {
        private ComprehensiveSearcher searcher;

        public SearchController(ComprehensiveSearcher searcher)
        {
            this.searcher = searcher;
        }

        [HttpGet]
        public SearchResultModel Index(string keyword, string requestId)
        {
            return this.searcher.GetSearchResult(requestId, keyword);
        }

        [HttpGet]
        public int Count(string requestId)
        {
            return this.searcher.GetSearchResultById(requestId)?.Searched ?? 0;
        }
    }
}