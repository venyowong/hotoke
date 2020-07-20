using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hotoke.Core.AspNetCore;
using Serilog.Core;
using Serilog.Events;
using Serilog;
using System.IO;

namespace Hotoke
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddHotoke(this.Configuration["searcher"], this.Configuration.GetSection("CustomSearcher"));

            services.AddMvc(option => option.EnableEndpointRouting = false);

            services.AddOptions()
                .Configure<AppSettings>(this.Configuration)
                .AddCors(o => o.AddPolicy("Default", builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var logSwitch = new LoggingLevelSwitch();
            if (env.IsDevelopment())
            {
                logSwitch.MinimumLevel = LogEventLevel.Information;
                app.UseDeveloperExceptionPage();
            }
            else
            {
                logSwitch.MinimumLevel = LogEventLevel.Warning;
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.Logger(lc =>
                {
                    lc.WriteTo
                        .RollingFile(Path.Combine(this.Configuration["Serilog:BaseFilePath"], "logs/info/{Hour}.txt"))
                        .Filter.ByIncludingOnly(@e => @e.Level == LogEventLevel.Information);
                })
                .WriteTo.Logger(lc =>
                {
                    lc.WriteTo
                        .RollingFile(Path.Combine(this.Configuration["Serilog:BaseFilePath"], "logs/warn/{Hour}.txt"))
                        .Filter.ByIncludingOnly(@e => @e.Level == LogEventLevel.Warning);
                })
                .WriteTo.Logger(lc =>
                {
                    lc.WriteTo
                        .RollingFile(Path.Combine(this.Configuration["Serilog:BaseFilePath"], "logs/error/{Hour}.txt"))
                        .Filter.ByIncludingOnly(@e => @e.Level == LogEventLevel.Error);
                })
                .WriteTo.Http(this.Configuration["Serilog:Http"])
                .CreateLogger();

            var defaultFile = new DefaultFilesOptions();  
            defaultFile.DefaultFileNames.Clear();  
            defaultFile.DefaultFileNames.Add("index.html");  
            app.UseDefaultFiles(defaultFile)
                .UseStaticFiles()
                .UseCookiePolicy()
                .UseCors("Default");

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
