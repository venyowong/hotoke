using System;
using System.Net;
using System.Net.Http;

namespace Hotoke.Common.HttpClientFactories
{
    /// <summary>
    /// 连续产生不同具有代理能力的实例的工厂
    /// </summary>
    public class ContinuousProxyFactory : IHttpClientFactory
    {
        private Func<string> proxyProducer;
        private static HttpClient _httpClient = new HttpClient();

        public ContinuousProxyFactory(Func<string> proxyProducer)
        {
            if(proxyProducer == null)
            {
                throw new ArgumentNullException(nameof(proxyProducer));
            }

            this.proxyProducer = proxyProducer;
        }

        public HttpClient GetHttpClient()
        {
            var proxy = this.proxyProducer();

            if(!string.IsNullOrWhiteSpace(proxy))
            {
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxy, false),
                    UseProxy = true
                };
                return new HttpClient(handler);
            }
            else
            {
                return _httpClient;
            }
        }
    }
}