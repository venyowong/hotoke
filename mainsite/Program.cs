using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Web;
using OpenTracing;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Util;
using Petabridge.Tracing.Zipkin;

namespace MainSite
{
    public class Program
    {
        private static readonly ZipkinTracer _tracer = new ZipkinTracer(
            new ZipkinTracerOptions(ConfigurationManager.AppSettings["ZipkinHost"], "hotoke"));

        public static void Main(string[] args)
        {
            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                GlobalTracer.Register(_tracer);
                CreateWebHostBuilder(args).Build().Run();
            }
            catch(Exception e)
            {
                logger.Error(e, "Stopped program because of exception");
            }
            finally
            {
                NLog.LogManager.Shutdown();
                _tracer.Dispose();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(ConfigurationManager.AppSettings["host"] ?? "http://0.0.0.0:80")
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                })
                .ConfigureServices(services => services.AddSingleton<ITracer>(_tracer))
                .UseNLog();
    }
}
