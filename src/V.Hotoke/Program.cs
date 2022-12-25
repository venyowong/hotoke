using Serilog.Events;
using Serilog;
using V.Talog.Client;
using V.Hotoke.Engines;
using V.SwitchableCache;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("log/log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

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
    // �ѹ��з������ƣ��������Ŀ��Ŀ�Ĳ���Ϊ��һ��Ҫ��ȡ��������Ľ����ֻ��Ϊ��ʹ�÷��㣬�Ѷ����������Ľ����������
    // ��Ȼ���������з������ƣ�����������������ǲ�ϣ��������ȡ��������ģ���˶����з������Ƶ����棬����Ŀ������ȥʹ��
    // �������ʹ�ø��ַ�ʽȥ�ƹ��������ƣ�Ҳ�������Ӻ�ʱ��Ӱ�������ٶ�
    .AddTransient<ISearchEngine, QuarkSmEngine>()
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
