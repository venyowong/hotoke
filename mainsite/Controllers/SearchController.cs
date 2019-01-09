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
    }
}