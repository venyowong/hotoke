using Hotoke.Search;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niolog;
using Niolog.Interfaces;

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

            services.AddSingleton<ComprehensiveSearcher>();

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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, 
            IOptions<AppSettings> appSettings, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if(appSettings?.Value?.Niolog != null)
            {
                NiologManager.DefaultWriters = new ILogWriter[]
                {
                    new ConsoleLogWriter(),
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

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
