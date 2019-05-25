using System.Net.Http;

namespace Hotoke.Common.HttpClientFactories
{
    public interface IHttpClientFactory
    {
        HttpClient GetHttpClient();
    }
}