using System.Collections.Generic;
using Hotoke.Common.Entities;

namespace Hotoke.Common
{
    public interface ISearchEngine
    {
        string Name{get;}
        IEnumerable<SearchResult> Search(string keyword, bool english = false);
    }
}