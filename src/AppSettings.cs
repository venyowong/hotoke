namespace Hotoke
{
    public class AppSettings
    {
        public NiologConfig Niolog{get;set;}
    }

    public class NiologConfig
    {
        public string Path{get;set;}
        public string Url{get;set;}
    }
}