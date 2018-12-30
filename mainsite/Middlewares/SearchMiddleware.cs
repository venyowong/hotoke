using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hotoke.Common;
using Hotoke.MainSite.Extensions;
using Hotoke.SearchEngines;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Util;

namespace Hotoke.MainSite.Middlewares
{
    public class SearchMiddleware
    {
        private readonly RequestDelegate next;

        private readonly List<ISearchEngine> engines = new List<ISearchEngine>();

        private readonly ILogger<SearchMiddleware> logger;

        public SearchMiddleware(RequestDelegate next, IConfiguration configuration, 
            ILogger<SearchMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
            this.engines = configuration["Engines"].Split(',').Select(engine => 
            {
                switch(engine)
                {
                    case "bing":
                        return new BingSearch();
                    case "baidu":
                        return new BaiduSearch();
                    case "google":
                        return new GoogleSearch();
                    case "hotoke":
                        return new HotokeSearch();
                    default:
                        return default(ISearchEngine);
                }
            })
            .Where(engine => engine != null)
            .ToList();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws/search")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await Search(context, webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await this.next(context);
            }
        }

        private async Task Search(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var request = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!request.CloseStatus.HasValue)
            {
                var keyword = Encoding.UTF8.GetString(buffer, 0, request.Count);
                var span = GlobalTracer.Instance?.BuildSpan("ws-search")
                    .WithTag("keyword", keyword)
                    .StartActive();
                this.logger.RecordInfo($"keyword: {keyword}, ip: {context.Connection.RemoteIpAddress}, useragent: {context.Request.Headers["User-Agent"]}");

                this.Search(webSocket, keyword);
                span?.Dispose();

                request = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }

        private void Search(WebSocket webSocket, string keyword)
        {
            var results = new List<SearchResult>();
            var spinLock = new SpinLock();
            var english = !keyword.HasOtherLetter();

            Parallel.ForEach(this.engines, engine =>
            {
                try
                {
                    var searchResults = engine.Search(keyword, english);
                    if(searchResults == null)
                    {
                        return;
                    }

                    var gotLock = false;
                    try
                    {
                        this.logger.RecordInfo($"count of {engine.Name} results: {searchResults.Count()}");
                        spinLock.Enter(ref gotLock);
                        this.MergeResult(keyword, searchResults, results);
                        this.logger.RecordInfo($"{engine.Name} results merged.");
                        webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(results))), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    finally
                    {
                        if(gotLock)
                        {
                            spinLock.Exit();
                        }
                    }
                }
                catch(Exception e)
                {
                    this.logger.RecordError(e, $"An exception occurred while searching for {keyword}");
                }
            });
        }

        private void MergeResult(string keyword, IEnumerable<SearchResult> searchResults, List<SearchResult> results)
        {
            if(results.Count == 0)
            {
                results.AddRange(searchResults);
                results.ForEach(result => 
                {
                    result.Sources.Add(result.Source);
                    var max = Math.Max(keyword.Length, result.Title.Length) + 1;
                    var diff = StringUtility.LevenshteinDistance(keyword, result.Title) + 1;
                    result.Score *= (float)diff / (float)max;
                });
            }
            else
            {
                var newResults = new List<SearchResult>();
                foreach(var result in searchResults)
                {
                    bool same = false;
                    foreach(var r in results)
                    {
                        if(r.Uri.SameAs(result.Uri) || r.Title == result.Title || (r.Title.Length <= 15 && 
                            result.Title.Length <= 15 && r.Title.SimilarWith(result.Title)))
                        {
                            same = true;

                            if(r.Score <= result.Score)
                            {
                                r.Score *= result.Score / result.Base;
                            }
                            else
                            {
                                r.Score = result.Score * (r.Score / r.Base);
                            }

                            if(!r.Sources.Contains(result.Source))
                            {
                                r.Sources.Add(result.Source);
                            }
                            break;
                        }
                    }

                    if(!same)
                    {
                        result.Sources.Add(result.Source);
                        var max = Math.Max(keyword.Length, result.Title.Length) + 1;
                        var diff = StringUtility.LevenshteinDistance(keyword, result.Title) + 1;
                        result.Score *= (float)diff / (float)max;
                        newResults.Add(result);
                    }
                }

                results.AddRange(newResults);
                newResults = results.OrderBy(result => result.Score).ToList();
                results.Clear();
                results.AddRange(newResults);
            }
        }
    }
}