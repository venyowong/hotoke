using System.Net.Http;

namespace Patu.HttpClientFactories
{
    public interface IHttpClientFactory
    {
        HttpClient GetHttpClient();
    }
}