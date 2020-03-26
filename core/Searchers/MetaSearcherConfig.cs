using System.Collections.Generic;
using Hotoke.Core.Engines;

namespace Hotoke.Core.Searchers
{
    public class MetaSearcherConfig
    {
        private Dictionary<string, ISearchEngine> engineMapping = new Dictionary<string, ISearchEngine>();

        public MetaSearcherConfig MapSearchEngine(string name, ISearchEngine searchEngine)
        {
            if (this.engineMapping.ContainsKey(name))
            {
                this.engineMapping[name] = searchEngine;
            }
            else
            {
                this.engineMapping.Add(name, searchEngine);
            }

            return this;
        }

        public ISearchEngine GetSearchEngine(string name)
        {
            if (this.engineMapping.ContainsKey(name))
            {
                return this.engineMapping[name];
            }

            return null;
        }
    }
}