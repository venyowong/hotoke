using System.Collections.Generic;
using Hotoke.Search;

namespace Hotoke.Models
{
    public class SearchResultModel
    {
        public string RequestId{get;set;}

        public int Searched{get;set;}

        public bool Finished{get;set;}

        public List<SearchResult> Results{get;set;}
    }
}