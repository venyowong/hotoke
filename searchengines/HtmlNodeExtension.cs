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

        public static HtmlNodeCollection SelectAllNodes(this HtmlNode node, string selection)
        {
            if(string.IsNullOrWhiteSpace(selection))
            {
                return null;
            }

            HtmlNodeCollection result = null;
            foreach(var item in selection.Split(';'))
            {
                result = result.AddRange(node.SelectNodes(item));
            }

            return result;
        }

        public static HtmlNode SelectFirstNode(this HtmlNode node, string selection)
        {
            if(string.IsNullOrWhiteSpace(selection))
            {
                return null;
            }

            foreach(var item in selection.Split(';'))
            {
                var result = node.SelectSingleNode(item);
                if(result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}