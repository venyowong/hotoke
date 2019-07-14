using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExtCore.WebApplication.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niolog;
using Niolog.Interfaces;

namespace Hotoke.MainSite
{
    public class Startup
    {
        private string extensionsPath;

        public Startup(IHostingEnvironment hostingEnvironment, IConfiguration configuration)
        {
            Configuration = configuration;
            this.extensionsPath = Path.Combine(hostingEnvironment.ContentRootPath, configuration["Extensions:Path"]);
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

            services.AddExtCore(this.extensionsPath, true);

            services.AddMvc();

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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IOptions<AppSettings> appSettings, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            if(appSettings?.Value?.Niolog != null)
            {
                NiologManager.DefaultWriters = new ILogWriter[]
                {
                    new FileLogWriter(appSettings.Value.Niolog.Path, 10),
                    new HttpLogWriter(appSettings.Value.Niolog.Url, 10, 1)
                };
            }

            loggerFactory.AddProvider(new LoggerProvider());

            var defaultFile = new DefaultFilesOptions();  
            defaultFile.DefaultFileNames.Clear();  
            defaultFile.DefaultFileNames.Add("index.html");  
            app.UseDefaultFiles(defaultFile)
                .UseStaticFiles()
                .UseCookiePolicy()
                .UseCors("Default");

            app.UseExtCore();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
