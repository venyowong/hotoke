using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Hotoke.Common.HttpClientFactories;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Hotoke.Common
{
    public static class HttpUtility
    {
        private static bool _useProxy = false;
        private static IHttpClientFactory _httpClientFactory = new ContinuousProxyFactory(() =>
        {
            return Get<JObject>(ConfigurationManager.AppSettings["ProxyPoolUrl"])?["http"]?.ToString();
        });
        private static HttpClient _httpClient = new HttpClient();
        public static HttpClient HttpClient
        {
            get
            {
                HttpClient httpClient = null;
                if(_useProxy)
                {
                    httpClient = _httpClientFactory.GetHttpClient();
                } 
                if(httpClient == null)
                {
                    httpClient = _httpClient;
                }

                return httpClient;
            }
        }
        private static JsonSerializerSettings JsonSettings;
        private static Random Random = new Random();
        private static Regex CharSetRegex = new Regex("charset=\"([^\"]+)", RegexOptions.IgnoreCase);
        private static string[] UserAgents = 
        {
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:16.0) Gecko/20121026 Firefox/16.0",
            "Mozilla/5.0 (iPad; U; CPU OS 4_2_1 like Mac OS X; zh-cn) AppleWebKit/533.17.9 (KHTML, like Gecko) Version/5.0.2 Mobile/8C148 Safari/6533.18.5",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:2.0b13pre) Gecko/20110307 Firefox/4.0b13pre",
            "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:16.0) Gecko/20100101 Firefox/16.0",
            "Mozilla/5.0 (Windows; U; Windows NT 6.1; zh-CN; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11",
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11",
            "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/534.16 (KHTML, like Gecko) Chrome/10.0.648.133 Safari/534.16",
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Win64; x64; Trident/5.0)",
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)",
            "Mozilla/5.0 (X11; U; Linux x86_64; zh-CN; rv:1.9.2.10) Gecko/20100922 Ubuntu/10.10 (maverick) Firefox/3.6.10",
            "Mozilla/5.0 (Linux; U; Android 2.2.1; zh-cn; HTC_Wildfire_A3333 Build/FRG83D) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1",
        };
        private static ConcurrentDictionary<string, string> Cookies = new ConcurrentDictionary<string, string>();

        static HttpUtility()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            JsonSettings = new JsonSerializerSettings();
            JsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            JsonSettings.Formatting = Formatting.Indented;

            bool.TryParse(ConfigurationManager.AppSettings["useproxy"], out _useProxy);
        }

        public static string Get(Uri uri, HttpClient client = null)
        {
            if(client == null)
            {
                client = HttpClient;
            }

            var request = new HttpRequestMessage() 
            {
                RequestUri = uri,
                Method = HttpMethod.Get,
            };
            request.Headers.Add("User-Agent", UserAgents[Random.Next(UserAgents.Length)]);
            if(Cookies.ContainsKey(uri.Host))
            {
                request.Headers.Add("Cookie", Cookies[uri.Host]);
            }
            using(var responseMessage = client.GetAsync(uri.AbsoluteUri).Result)
            {
                Cookies.TryGetValue(uri.Host, out string cookie);
                var oldCookie = cookie;
                cookie = responseMessage.Headers.CollectCookie("Cookie", cookie);
                cookie = responseMessage.Headers.CollectCookie("Set-Cookie", cookie);
                if(Cookies.ContainsKey(uri.Host))
                {
                    Cookies.TryUpdate(uri.Host, cookie, oldCookie);
                }
                else
                {
                    Cookies.TryAdd(uri.Host, cookie);
                }

                var charset = responseMessage?.Content?.Headers?.ContentType?.CharSet;
                var content = responseMessage?.Content?.ReadAsStringAsync().Result;
                try
                {
                    if(string.IsNullOrWhiteSpace(charset))
                    {
                        var match = CharSetRegex.Match(content);
                        if(match != null && match.Success)
                        {
                            responseMessage.Content.Headers.ContentType.CharSet = match.Groups[1].Value;
                        }
                    }
                    return responseMessage.Content.ReadAsStringAsync().Result;
                }
                catch
                {
                    return content;
                }
            }
        }

        public static T Get<T>(string url)
        {
            var json = Get(new Uri(url));
            if(string.IsNullOrWhiteSpace(json))
            {
                return default(T);
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch(Exception)
            {
                return default(T);
            }
        }

        public static string Get(string url)
        {
            return Get(new Uri(url));
        }

        private static string CollectCookie(this HttpHeaders headers, string key, string cookie)
        {
            headers.TryGetValues(key, out IEnumerable<string> cookies);
            if(cookies != null)
            {
                if(string.IsNullOrWhiteSpace(cookie))
                {
                    cookie = string.Join(";", cookies);
                }
                else
                {
                    cookie += ';' + string.Join(";", cookies);
                }
            }

            return cookie;
        }

        public static bool SameAs(this Uri uri, Uri otherUri)
        {
            if(uri?.Host == otherUri?.Host && uri?.PathAndQuery == otherUri?.PathAndQuery)
            {
                return true;
            }

            return false;
        }

        public static string Post(string url, IEnumerable<KeyValuePair<string, string>> data)
        {
            if(string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            return HttpClient.PostAsync(new Uri(url), 
                data != null ? new FormUrlEncodedContent(data) : null)
                ?.Result?.Content?.ReadAsStringAsync()?.Result;
        }

        public static string PostJson(string url, object data)
        {
            if(string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            return HttpClient.PostAsync(new Uri(url), 
                data != null ? new StringContent(JsonConvert.SerializeObject(data), 
                Encoding.UTF8, "application/json") : null)?.Result?.Content
                ?.ReadAsStringAsync()?.Result;
        }
    }
}