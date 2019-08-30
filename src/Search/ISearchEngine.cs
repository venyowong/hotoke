using System.Collections.Generic;

namespace Hotoke.Search
{
    public interface ISearchEngine
    {
        string Name{get;}
        
        IEnumerable<SearchResult> Search(string keyword, bool english = false);
    }
}