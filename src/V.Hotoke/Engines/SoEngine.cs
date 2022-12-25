using V.Talog.Client;

namespace V.Hotoke.Engines
{
    public class SoEngine : GenericEngine
    {
        public SoEngine(IConfiguration config, IHttpClientFactory clientFactory, LogChannel logChannel)
            : base("360", config, clientFactory, logChannel)
        {
        }

        public override string GetSearchUrl(string keyword, int pageIndex, bool english)
        {
            var pn = pageIndex + 1;
            return this.baseUrl.Replace("{keyword}", keyword).Replace("{pn}", pn.ToString());
        }
    }
}
