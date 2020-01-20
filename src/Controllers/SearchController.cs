using System.ComponentModel.DataAnnotations;
using System.Threading;
using Hotoke.Models;
using Hotoke.Search;
using Microsoft.AspNetCore.Mvc;

namespace Hotoke.Controllers
{
    public class SearchController : Controller
    {
        private BaseMetaSearcher searcher;

        public SearchController(BaseMetaSearcher searcher)
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
            return this.searcher.GetSearchResult(requestId, string.Empty)?.Searched ?? 0;
        }

        [HttpGet("/search/engines")]
        public object GetEngines()
        {
            return this.searcher.GetEngineList();
        }

        [HttpGet("/search/all")]
        public object SearchAllResults(string keyword, string requestId)
        {
            return this.searcher.GetSearchResult(requestId, keyword, false);
        }
    }
}