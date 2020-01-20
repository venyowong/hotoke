using System.Collections.Generic;

namespace Hotoke.Search
{
    public interface ISearchEngine
    {
        string Name{get;}

        float Weight{get;set;}
        
        IEnumerable<SearchResult> Search(string keyword, bool english = false);
    }
}