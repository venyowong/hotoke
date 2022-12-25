using V.Hotoke.Models;

namespace V.Hotoke.Engines
{
    public interface ISearchEngine
    {
        string Name { get; }

        Task<List<SearchResult>> Search(string keyword, int pageIndex, bool english);
    }
}
