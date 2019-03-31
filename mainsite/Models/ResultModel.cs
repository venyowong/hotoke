namespace Hotoke.MainSite.Models
{
    public class ResultModel
    {
        public bool Success {get;set;}
        public string Message{get;set;}
    }

    public class ResultModel<T> : ResultModel
    {
        public T Result{get;set;}
    }
}