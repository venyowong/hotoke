using Newtonsoft.Json;
using V.Common.Extensions;

namespace V.Hotoke.Models
{
    public class SearchResult
    {
        public string Title { get; set; }

        private List<KeyValuePair<char, int>> titleHash = null;
        [JsonIgnore]
        public List<KeyValuePair<char, int>> TitleHash
        {
            get
            {
                if (titleHash == null)
                {
                    var dic = new Dictionary<char, int>();
                    foreach (var c in this.Title)
                    {
                        if (dic.TryGetValue(c, out int times))
                        {
                            dic[c] = times + 1;
                        }
                        else
                        {
                            dic.Add(c, 1);
                        }
                    }
                    this.titleHash = dic.OrderBy(x => x.Key).ToList();
                }

                return titleHash;
            }
        }

        public string Url { get; set; }

        private Uri uri;
        [JsonIgnore]
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

        public string Source { get; set; }

        public List<string> Sources { get; set; } = new List<string>();

        public bool SimilarWith(SearchResult result)
        {
            if (this.Title == result.Title || this.Url == result.Url)
            {
                return true;
            }
            if (this.TitleHash.IsNullOrEmpty() || result.TitleHash.IsNullOrEmpty())
            {
                return false;
            }

            var maxLen = this.Title.Length;
            if (result.Title.Length > maxLen)
            {
                maxLen = result.Title.Length;
            }
            var i1 = 0;
            var i2 = 0;
            double diff = 0;
            while (i1 < this.TitleHash.Count || i2 < result.TitleHash.Count)
            {
                char c1;
                if (i1 < this.TitleHash.Count)
                {
                    c1 = this.TitleHash[i1].Key;
                }
                else
                {
                    c1 = this.TitleHash[this.TitleHash.Count - 1].Key;
                }
                char c2;
                if (i2 < result.TitleHash.Count)
                {
                    c2 = result.TitleHash[i2].Key;
                }
                else
                {
                    c2 = result.TitleHash[result.TitleHash.Count - 1].Key;
                }

                if (c1 == c2)
                {
                    diff += Math.Pow(this.TitleHash[i1].Value - result.TitleHash[i2].Value, 2);
                    i1++;
                    i2++;
                    continue;
                }

                if ((c1 < c2 && i1 < this.TitleHash.Count) || i2 >= result.TitleHash.Count)
                {
                    diff += Math.Pow(this.TitleHash[i1].Value, 2);
                    i1++;
                    continue;
                }

                if ((c1 > c2 && i2 < result.TitleHash.Count) || i1 >= this.TitleHash.Count)
                {
                    diff += Math.Pow(result.TitleHash[i2].Value, 2);
                    i2++;
                    continue;
                }
            }
            diff = Math.Sqrt(diff);

            return diff <= maxLen / 10.0;
        }

        public void UpdateUrl(string url)
        {
            this.Url = url;
            this.uri= new Uri(url);
        }
    }
}
