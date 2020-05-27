using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hotoke.Core.AspNetCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Hotoke.Aliyun
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
            services.AddHotoke(this.Configuration["searcher"], this.Configuration.GetSection("CustomSearcher"));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddCors(o => o.AddPolicy("Default", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
                app.UseHsts();
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(logSwitch)
                .WriteTo.Console()
                .WriteTo.Http(this.Configuration["serilog:http"])
                .Enrich.FromLogContext()
                .CreateLogger();

            app.UseCors("Default");
            app.UseHttpsRedirection();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
