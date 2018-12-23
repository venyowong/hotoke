using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using OpenTracing.Util;

namespace OpentracingExtension
{
    public class HttpClientDiagnosticProcessor : IDiagnosticProcessor
    {
        private ConcurrentDictionary<string, ISpan> requests = new ConcurrentDictionary<string, ISpan>();

        public string ListenerName => "HttpHandlerDiagnosticListener";

        public List<string> IgnorePattern{get;set;} = new List<string>();

        [DiagnosticName("System.Net.Http.Request")]
        public void HttpRequest(object value)
        {
            var request = value.GetProperty<HttpRequestMessage>("Request");
            var requestId = value.GetProperty<Guid>("LoggingRequestId");
            if(request == null)
            {
                return;
            }

            if(request.RequestUri.AbsolutePath.Contains("/api/v2/spans"))
            {
                return;
            }

            foreach(var pattern in this.IgnorePattern)
            {
                if(request.RequestUri.AbsoluteUri.Contains(pattern))
                {
                    return;
                }
            }

            var span = GlobalTracer.Instance.BuildSpan("http request")
                .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                .WithTag(Tags.HttpMethod, request.Method.ToString())
                .WithTag(Tags.HttpUrl, request.RequestUri.ToString())
                .WithTag(Tags.PeerHostname, request.RequestUri.Host)
                .WithTag(Tags.PeerPort, request.RequestUri.Port)
                .Start();
            GlobalTracer.Instance.Inject(span.Context, BuiltinFormats.HttpHeaders, new HttpHeadersInjectAdapter(request.Headers));
            this.requests.TryAdd(requestId.ToString(), span);
        }

        [DiagnosticName("System.Net.Http.Response")]
        public void HttpResponse(object value)
        {
            var requestId = value.GetProperty<Guid>("LoggingRequestId");
            var response = value.GetProperty<HttpResponseMessage>("Response");

            if(response == null)
            {
                return;
            }

            if(!this.requests.TryGetValue(requestId.ToString(), out ISpan span))
            {
                return;
            }

            span.SetTag(Tags.HttpStatus, (int)response.StatusCode);
            span.Finish();
        }
    }
}