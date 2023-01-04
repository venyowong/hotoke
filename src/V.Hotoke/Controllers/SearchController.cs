using Microsoft.AspNetCore.Mvc;
using V.Hotoke.Engines;
using V.Hotoke.Models;
using V.Talog.Client;

namespace V.Hotoke.Controllers
{
    [ApiController]
    [Route("search")]
    public class SearchController
    {
        [HttpGet]
        [Route("meta")]
        public async Task<PagedResult<SearchResult>> MetaSearch([FromQuery] string keyword, [FromQuery] int page, [FromServices] MetaSearcher service)
        {
            PageViewSender.Enqueue("V.Hotoke", "/search/meta", null);
            return await service.Search(keyword, page - 1);
        }
    }
}
