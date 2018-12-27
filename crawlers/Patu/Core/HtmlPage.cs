using System;
using HtmlAgilityPack;

namespace Patu
{
    public class HtmlPage
    {
        public string Url{get;set;}
        public Uri Uri{get;set;}
        /// <summary>
        /// html content
        /// </summary>
        /// <returns></returns>
        public string Content{get;set;}
        /// <summary>
        /// html document
        /// </summary>
        /// <returns></returns>
        public HtmlDocument Document{get;set;}

        /// <summary>
        /// equals with this.Document.DocumentNode.SelectNodes(xpath)
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public HtmlNodeCollection SelectNodes(string xpath)
        {
            return this.Document?.DocumentNode.SelectNodes(xpath);
        }

        /// <summary>
        /// equals with this.Document.DocumentNode.SelectSingleNode(xpath)
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public HtmlNode SelectSingleNode(string xpath)
        {
            return this.Document?.DocumentNode.SelectSingleNode(xpath);
        }
    }
}