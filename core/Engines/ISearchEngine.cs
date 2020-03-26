using System.Collections.Generic;
using Hotoke.Core.Models;

namespace Hotoke.Core.Engines
{
    public interface ISearchEngine
    {
        string Name{get;}

        float Weight{get;set;}
        
        IEnumerable<SearchResult> Search(string keyword, bool english = false);
    }
}