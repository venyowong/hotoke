using HtmlAgilityPack;

namespace Hotoke.SearchEngines
{
    public static class HtmlNodeExtension
    {
        public static HtmlNodeCollection AddRange(this HtmlNodeCollection collection, HtmlNodeCollection other)
        {
            if(other == null)
            {
                return collection;
            }

            if(collection == null)
            {
                return other;
            }

            foreach(var node in other)
            {
                collection.Add(node);
            }

            return collection;
        }
    }
}