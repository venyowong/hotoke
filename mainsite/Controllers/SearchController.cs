using System.Collections.Generic;
using Hotoke.Common;
using Hotoke.MainSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hotoke.MainSite.Controllers
{
    public class SearchController : Controller
    {
        private IEnumerable<ISearchEngine> engines;

        public SearchController(IEnumerable<ISearchEngine> engines)
        {
            this.engines = engines;
        }

        [HttpGet]
        public SearchResultModel Index(string keyword, string requestId)
        {
            return new SearchManager(this.engines).GetSearchResult(requestId, keyword);
        }

        [HttpGet]
        public int Count(string requestId)
        {
            return new SearchManager(this.engines).GetSearchResultById(requestId)?.Searched ?? 0;
        }

        [HttpGet]
        public object Engines()
        {
            return this.engines;
        }
    }
}