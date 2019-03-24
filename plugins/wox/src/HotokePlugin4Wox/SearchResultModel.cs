using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotokePlugin4Wox
{
    public class SearchResultModel
    {
        public string RequestId { get; set; }

        public int Searched { get; set; }

        public bool Finished { get; set; }

        public List<SearchResult> Results { get; set; }
    }

    public class SearchResult
    {
        public string Title { get; set; }

        public string Url { get; set; }

        private Uri uri;
        public Uri Uri
        {
            get
            {
                if (this.uri == null && !string.IsNullOrWhiteSpace(this.Url))
                {
                    this.uri = new Uri(this.Url);
                }

                return this.uri;
            }
        }

        public string Desc { get; set; }

        public float Score { get; set; }

        public float Base { get; set; }

        public string Source { get; set; }

        public List<string> Sources { get; set; } = new List<string>();
    }
}
