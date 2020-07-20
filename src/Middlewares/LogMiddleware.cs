using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace Hotoke.Middlewares
{
    public class LogMiddleware
    {
        private readonly RequestDelegate next;

        public LogMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
            {
                try
                {
                    await this.next(context);
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    Log.Error(e, "an error occure");
                    await context.Response.WriteAsync("an error occure");
                }
            }
        }
    }
}