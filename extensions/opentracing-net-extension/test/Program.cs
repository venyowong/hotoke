using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Tag;
using OpenTracing.Util;
using Petabridge.Tracing.Zipkin;
using Petabridge.Tracing.Zipkin.Sampling;

namespace OpentracingExtension.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var observer = new DiagnosticListenerObserver();
            observer.AddProcessor(new HttpClientDiagnosticProcessor());
            DiagnosticListener.AllListeners.Subscribe(observer);

            var tracer = new ZipkinTracer(new ZipkinTracerOptions("http://venyo.cn:9411", "hotoke")
            {
                ScopeManager = new AsyncLocalScopeManager()
            });
            GlobalTracer.Register(tracer);

            var scope = tracer.BuildSpan("test").StartActive(true);
                    var json = new HttpClient().GetStringAsync("http://venyo.cn:3289/count").Result;
                    Console.WriteLine(tracer.ActiveSpan == scope.Span);
                    scope.Dispose();

            Thread.Sleep(500);
            tracer.Dispose();
            
            Console.WriteLine("Hello World!");
        }
    }
}
