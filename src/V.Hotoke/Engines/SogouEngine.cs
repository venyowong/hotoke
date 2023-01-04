using V.Talog.Client;

namespace V.Hotoke.Engines
{
    public class SogouEngine : GenericEngine
    {
        public SogouEngine(IConfiguration config, IHttpClientFactory clientFactory, LogChannel logChannel) : base("Sogou", config, clientFactory, logChannel)
        {
        }

        public override string GetSearchUrl(string keyword, int pageIndex, bool english)
        {
            var page = pageIndex + 1;
            return this.baseUrl.Replace("{keyword}", System.Web.HttpUtility.UrlEncode(keyword)).Replace("{page}", page.ToString());
        }
    }
}
