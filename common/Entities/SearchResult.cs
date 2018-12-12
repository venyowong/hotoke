using System;
using System.Collections.Generic;

namespace Hotoke.Common
{
    public class SearchResult
    {
        public string Title{get;set;}

        public string Url{get;set;}

        private Uri uri;
        public Uri Uri
        {
            get
            {
                if(this.uri == null && !string.IsNullOrWhiteSpace(this.Url))
                {
                    this.uri = new Uri(this.Url);
                }

                return this.uri;
            }
        }

        public string Desc{get;set;}

        public float Score{get;set;}

        public float Base{get;set;}

        public string Source{get;set;}

        public List<string> Sources{get;set;} = new List<string>();
    }
}