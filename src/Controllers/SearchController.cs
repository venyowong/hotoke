using System.ComponentModel.DataAnnotations;
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

        [HttpGet, ModelValidation]
        public int Count([Required]string requestId)
        {
            return this.searcher.GetSearchResultById(requestId)?.Searched ?? 0;
        }
    }
}