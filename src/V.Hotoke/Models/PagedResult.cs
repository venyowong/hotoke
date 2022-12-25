namespace V.Hotoke.Models
{
    public class PagedResult<T>
    {
        public int Code { get; set; }

        public string Msg { get; set; }

        public int Total { get; set; }

        public List<T> Items { get; set; }
    }
}
