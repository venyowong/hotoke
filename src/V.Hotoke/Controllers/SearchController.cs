using Microsoft.AspNetCore.Mvc;
using V.Hotoke.Engines;
using V.Hotoke.Models;

namespace V.Hotoke.Controllers
{
    [ApiController]
    [Route("search")]
    public class SearchController
    {
        [HttpGet]
        [Route("meta")]
        public Task<PagedResult<SearchResult>> MetaSearch([FromQuery] string keyword, [FromQuery] int page, [FromServices] MetaSearcher service) 
            => service.Search(keyword, page - 1);
    }
}
