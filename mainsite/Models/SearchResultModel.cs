using System.Collections.Generic;
using Hotoke.Common;

namespace Hotoke.MainSite.Models
{
    public class SearchResultModel
    {
        public string RequestId{get;set;}

        public int Searched{get;set;}

        public bool Finished{get;set;}

        public List<SearchResult> Results{get;set;}
    }
}