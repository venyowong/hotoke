using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hotoke.Core
{
    public static class HttpUtility
    {
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
        private static CookieContainer _cookieContainer = new CookieContainer();
        static HttpUtility()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static string Get(Uri uri)
        {
            var request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";
            request.Headers.Add("User-Agent", UserAgents[Random.Next(UserAgents.Length)]);
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Host", uri.Host);
            request.CookieContainer = _cookieContainer;
            request.Timeout = 5000;
            using (var response = request.GetResponse())
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                return streamReader.ReadToEnd();
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
                return JsonSerializer.Deserialize<T>(json);
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

        private static Regex HtmlUrlRegex = new Regex(@"^([a-zA-Z]+:)?(//)?([^/]+)?(/)?[^\s]*");
        public static bool IsUrl(Uri pageUri, ref string str)
        {
            if(string.IsNullOrWhiteSpace(str))
            {
                return false;
            }
            var match = HtmlUrlRegex.Match(str);
            if(match == null || !match.Success)
            {
                return false;
            }

            string prefix = null;
            if(string.IsNullOrEmpty(match.Groups[1].Value))
            {
                prefix = "http:";
            }
            else
            {
                return true;
            }

            if(string.IsNullOrEmpty(match.Groups[2].Value))
            {
                prefix += "//";
            }
            else
            {
                str = prefix + str;
                return true;
            }

            if(string.IsNullOrEmpty(match.Groups[3].Value))
            {
                prefix = pageUri.AbsoluteUri.Substring(0, pageUri.AbsoluteUri.Length - pageUri.AbsolutePath.Length);
            }
            else
            {
                str = prefix + str;
                return true;
            }

            if(!string.IsNullOrEmpty(match.Groups[4].Value))
            {
                var index = pageUri.AbsoluteUri.LastIndexOf('/');
                prefix = pageUri.AbsoluteUri.Substring(0, index);
            }
            else
            {
                str = prefix + '/' + str;
                return true;
            }

            str = prefix + str;
            return true;
        }
    }
}