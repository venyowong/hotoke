namespace Hotoke.MainSite
{
    public class AppSettings
    {
        public string SearchHost{get;set;}
        public MysqlConfig Mysql{get;set;}
        public JwtConfig Jwt{get;set;}
        public string EsHost{get;set;}
    }

    public class MysqlConfig
    {
        public string ConnectionString{get;set;}
    }

    public class JwtConfig
    {
        public string Host{get;set;}
    }
}