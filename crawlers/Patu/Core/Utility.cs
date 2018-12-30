using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Patu.Config;

namespace Patu
{
    public static class Utility
    {
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
        private static Random Random = new Random();
        private static ConcurrentDictionary<string, string> Cookies = new ConcurrentDictionary<string, string>();
        private static ILog _log = null;

        static Utility()
        {
            var repository = LogManager.CreateRepository("Patu");
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _log = LogManager.GetLogger("Patu", typeof(Utility));
        }

        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger("Patu", type);
        }

        public static void Sleep(long interval)
        {
            while(interval > 0)
            {
                if(interval > int.MaxValue)
                {
                    Thread.Sleep(int.MaxValue);
                    interval -= int.MaxValue;
                }
                else
                {
                    Thread.Sleep((int)interval);
                    interval = 0;
                }
            }
        }

        public static HttpClient HttpClient = new HttpClient();
        private static Regex CharSetRegex = new Regex("charset=\"([^\"]+)", RegexOptions.IgnoreCase);

        public static string FetchHtml(string url, HttpClient client = null)
        {
            return FetchHtml(new Uri(url), client);
        }

        public static string FetchHtml(Uri uri, HttpClient client = null)
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

        public static T HttpGet<T>(string url)
        {
            var json = FetchHtml(url);
            if(string.IsNullOrWhiteSpace(json))
            {
                return default(T);
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default(T);
            }
        }

        public static JObject HttpGet(string url)
        {
            return HttpGet<JObject>(url);
        }

        private static Regex UrlRegex = new Regex(@"[a-zA-z]+://[^\s]*");
        public static bool IsUrl(string str)
        {
            return UrlRegex.Match(str)?.Success ?? false;
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
                // http://xyq.163.com
                prefix = pageUri.AbsoluteUri.Substring(0, pageUri.AbsoluteUri.Length - pageUri.AbsolutePath.Length);
            }
            else
            {
                str = prefix + str;
                return true;
            }

            if(string.IsNullOrEmpty(match.Groups[4].Value))
            {
                var index = pageUri.AbsoluteUri.LastIndexOf('/');
                prefix = pageUri.AbsoluteUri.Substring(0, index + 1);
            }
            else
            {
                str = prefix + str;
                return true;
            }

            str = prefix + str;
            return true;
        }

        public static string ConvertToUtf8(string str, string encode)
        {
            if(string.IsNullOrWhiteSpace(str) || string.IsNullOrWhiteSpace(encode))
            {
                return str;
            }
            try
            {
                var encoding = Encoding.GetEncoding(encode);
                return Encoding.UTF8.GetString(Encoding.Convert(encoding, 
                    Encoding.UTF8, encoding.GetBytes(str)));
            }
            catch
            {
                return str;
            }
        }

        public static string ConvertFromUtf8(string str, string encode)
        {
            if(string.IsNullOrWhiteSpace(str) || string.IsNullOrWhiteSpace(encode))
            {
                return str;
            }
            try
            {
                var encoding = Encoding.GetEncoding(encode);
                return encoding.GetString(Encoding.Convert(Encoding.UTF8, 
                    encoding, Encoding.UTF8.GetBytes(str)));
            }
            catch
            {
                return str;
            }
        }

        public static bool ContainsAnySubstring(this string str, IEnumerable<string> list)
        {
            if(str == null || list == null)
            {
                return false;
            }

            foreach(var item in list)
            {
                if(str.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static void SendAutoDownMail(AutoDownConfig config, string subject, string body)
        {
            if(config == null)
            {
                _log.Info("AutoDownConfig is null, so won't send mail.");
                return;
            }

            try
            {
                using(var client = new SmtpClient())
                {
                    client.Host = config.SmtpHost;
                    if(config.SmtpPort > 0)
                    {
                        client.Port = config.SmtpPort;
                    }

                    var message = new MailMessage(config.SendMail, config.ReceiveMail);
                    message.Subject = subject;
                    message.SubjectEncoding = Encoding.UTF8;
                    message.Body = $@"Hi,Patu user

                    This is a Patu auto down mail.

                    {body}
                    
                    --Patu";
                    message.BodyEncoding = Encoding.UTF8;
                    config.CopyMails?.ForEach(mail => message.CC.Add(mail));
                    
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(config.SendMail, config.SendPassword);

                    client.Send(message);
                    _log.Info("Success to send auto down mail.");
                }
            }
            catch(Exception e)
            {
                _log.Error("Catched an exception when sending auto down mail", e);
            }
        }
    }
}