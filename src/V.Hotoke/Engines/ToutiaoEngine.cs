using System.Collections.Generic;
using V.Talog.Client;

namespace V.Hotoke.Engines
{
    public class ToutiaoEngine : GenericEngine
    {
        public ToutiaoEngine(IConfiguration config, IHttpClientFactory clientFactory, LogChannel logChannel) 
            : base("Toutiao", config, clientFactory, logChannel)
        {
        }

        public override string GetSearchUrl(string keyword, int pageIndex, bool english)
        {
            return this.baseUrl.Replace("{keyword}", System.Web.HttpUtility.UrlEncode(keyword))
                .Replace("{pageIndex}", pageIndex.ToString());
        }
    }
}
