using System.Collections.Generic;
using Hotoke.Models;
using Hotoke.Search;
using Microsoft.AspNetCore.Mvc;

namespace Hotoke.Controllers
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