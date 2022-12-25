using V.Talog.Client;

namespace V.Hotoke.Engines
{
    public class QuarkSmEngine : GenericEngine
    {
        public QuarkSmEngine(IConfiguration config, IHttpClientFactory clientFactory, LogChannel logChannel) : base("QuarkSm", config, clientFactory, logChannel)
        {
        }

        public override string GetSearchUrl(string keyword, int pageIndex, bool english)
        {
            var page = pageIndex + 1;
            return this.baseUrl.Replace("{keyword}", keyword).Replace("{page}", page.ToString());
        }
    }
}
