using System;
using Microsoft.Extensions.Logging;
using OpenTracing.Util;

namespace Hotoke.MainSite.Extensions
{
    public static class LoggerExtension
    {
        public static void RecordInfo(this ILogger logger, string message)
        {
            logger?.LogInformation(
                $"[{GlobalTracer.Instance?.ActiveSpan?.Context.TraceId},{GlobalTracer.Instance?.ActiveSpan?.Context.SpanId}] {message}");
            GlobalTracer.Instance?.ActiveSpan?.Log(message);
        }

        public static void RecordError(this ILogger logger, Exception exception, string message)
        {
            logger?.LogError(exception, 
                $"[{GlobalTracer.Instance?.ActiveSpan?.Context.TraceId},{GlobalTracer.Instance?.ActiveSpan?.Context.SpanId}] {message}");
            GlobalTracer.Instance?.ActiveSpan?.Log($"{message}\n{exception?.Message}\n{exception?.StackTrace}");
        }
    }
}