using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace OpentracingExtension
{
    public static class TraceExtension
    {
        [ThreadStatic]
        private static readonly Random _random = new Random();

        public static long GetTraceId(this HttpRequestHeaders headers)
        {
            if(headers == null)
            {
                return _random.NextLong(0, long.MaxValue);
            }

            headers.TryGetValues("traceid", out IEnumerable<string> values);
            var traceId = values?.FirstOrDefault();
            if(!string.IsNullOrWhiteSpace(traceId))
            {
                if(long.TryParse(traceId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }
            headers.TryGetValues("traceId", out values);
            if(!string.IsNullOrWhiteSpace(traceId))
            {
                if(long.TryParse(traceId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }
            headers.TryGetValues("Traceid", out values);
            if(!string.IsNullOrWhiteSpace(traceId))
            {
                if(long.TryParse(traceId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }
            headers.TryGetValues("TraceId", out values);
            if(!string.IsNullOrWhiteSpace(traceId))
            {
                if(long.TryParse(traceId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }

            return _random.NextLong(0, long.MaxValue);
        }

        public static long GetSpanId(this HttpRequestHeaders headers)
        {
            if(headers == null)
            {
                return _random.NextLong(0, long.MaxValue);
            }

            headers.TryGetValues("spanid", out IEnumerable<string> values);
            var spanId = values?.FirstOrDefault();
            if(!string.IsNullOrWhiteSpace(spanId))
            {
                if(long.TryParse(spanId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }
            headers.TryGetValues("spanId", out values);
            if(!string.IsNullOrWhiteSpace(spanId))
            {
                if(long.TryParse(spanId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }
            headers.TryGetValues("Spanid", out values);
            if(!string.IsNullOrWhiteSpace(spanId))
            {
                if(long.TryParse(spanId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }
            headers.TryGetValues("SpanId", out values);
            if(!string.IsNullOrWhiteSpace(spanId))
            {
                if(long.TryParse(spanId, out long result))
                {
                    return result;
                }
                else
                {
                    return _random.NextLong(0, long.MaxValue);
                }
            }

            return _random.NextLong(0, long.MaxValue);
        }

        public static long NextLong(this Random random, long min, long max) 
        {
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }
    }
}