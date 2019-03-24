using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Wox.Plugin;

namespace HotokePlugin4Wox
{
    public class Main : IPlugin
    {
        public void Init(PluginInitContext context)
        {
        }

        public List<Result> Query(Query query)
        {
            if(query?.ActionKeyword == "ho" && !string.IsNullOrWhiteSpace(query?.Search) &&
                query.Search.EndsWith("#"))
            {
                var keyword = query.Search.Substring(0, query.Search.Length - 1);
                bool finished = false;
                var requestId = string.Empty;
                SearchResultModel result = null;
                while (!finished)
                {
                    var url = $"http://venyo.cn/search?keyword={keyword}";
                    if(!string.IsNullOrWhiteSpace(requestId))
                    {
                        url = $"http://venyo.cn/search?requestId={requestId}";
                    }
                    var request = WebRequest.CreateHttp(url);
                    using (var response = request.GetResponse())
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var json = reader.ReadToEnd();
                        result = JsonConvert.DeserializeObject<SearchResultModel>(json);
                        finished = result.Finished;
                        requestId = result.RequestId;
                    }
                }

                if(result != null)
                {
                    return result.Results.Select(r => new Result
                    {
                        Title = $"{r.Title} from {string.Join(",", r.Sources)}",
                        SubTitle = r.Desc,
                        IcoPath = "logo.png",
                        Action = context =>
                        {
                            Process.Start(r.Url);
                            return true;
                        }
                    }).ToList();
                }
            }

            return null;
        }

        public void Log(string message)
        {
            using (var writer = new StreamWriter(new FileStream("D:\\wox\\plugin.log",
                FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
            {
                writer.WriteLine(message);
            }
        }
    }
}
