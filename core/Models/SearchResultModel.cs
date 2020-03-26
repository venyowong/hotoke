using System.Collections.Generic;

namespace Hotoke.Core.Models
{
    public class SearchResultModel
    {
        public string RequestId{get;set;}

        public int Searched{get;set;}

        public bool Finished{get;set;}

        public List<SearchResult> Results{get;set;}
    }
}