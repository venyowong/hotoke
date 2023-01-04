using Serilog.Events;
using Serilog;
using V.Talog.Client;
using V.Hotoke.Engines;
using V.SwitchableCache;
using V.Hotoke.Services;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("log/log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["ReportMetrics"]?.ToLower() == "true")
{
    builder.Services.AddHostedService<MetricsHostService>();
}

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Config.TalogServer = builder.Configuration["TalogServer"];
builder.Services.AddSingleton(new LogChannel("V.Hotoke", 
    new Dictionary<string, string> 
    {
        { "env", builder.Environment.EnvironmentName }
    }, 1, "hotoke"));


builder.Services.AddSwitchableCache();
builder.Services.AddHttpClient()
    .AddTransient<ISearchEngine, SoEngine>()
    .AddTransient<ISearchEngine, BingEngine>()
    //.AddTransient<ISearchEngine, SogouEngine>() 
    // 搜狗有反爬机制，做这个项目的目的不是为了一定要爬取搜索引擎的结果，只是为了使用方便，把多个搜索引擎的结果整合起来
    // 既然搜索引擎有反爬机制，代表这个搜索引擎是不希望别人爬取搜索结果的，因此对于有反爬机制的引擎，本项目都不会去使用
    // 而且如果使用各种方式去绕过反爬机制，也都会增加耗时，影响搜索速度
    .AddTransient<ISearchEngine, QuarkSmEngine>()
    //.AddTransient<ISearchEngine, ToutiaoEngine>()
    // 头条搜索需要使用 Cookie 才能返回正常结果，但是元搜索引擎有一点就是希望帮用户减少隐私泄露，因此不能启用 Cookie 功能，并且启用之后搜索结果会受到所有使用者的影响
    .AddTransient<MetaSearcher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
