using Hotoke.MainSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hotoke.MainSite.Controllers
{
    public class SearchController : Controller
    {
        [HttpGet]
        public SearchResultModel Index(string keyword, string requestId)
        {
            return SearchManager.GetSearchResult(requestId, keyword);
        }

        [HttpGet]
        public int Count(string requestId)
        {
            return SearchManager.GetSearchResultById(requestId)?.Searched ?? 0;
        }
    }
}