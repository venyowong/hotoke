using V.Talog.Client;

namespace V.Hotoke.Engines
{
    public class BingEngine : GenericEngine
    {
        public BingEngine(IConfiguration config, IHttpClientFactory clientFactory, LogChannel logChannel)
            : base("Bing", config, clientFactory, logChannel)
        {
        }

        public override string GetSearchUrl(string keyword, int pageIndex, bool english)
        {
            var ensearch = english ? "1" : "0";
            var first = pageIndex * 10 + 1;
            return this.baseUrl.Replace("{keyword}", System.Web.HttpUtility.UrlEncode(keyword)).Replace("{first}", first.ToString()).Replace("{ensearch}", ensearch);
        }
    }
}
