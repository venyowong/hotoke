using System.Collections.Generic;
using Hotoke.Common;
using Hotoke.Common.Entities;

namespace Hotoke.SearchEngines
{
    public interface ISearchEngine
    {
        string Name{get;}
        IEnumerable<SearchResult> Search(string keyword, bool english = false);
    }
}