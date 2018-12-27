namespace Patu.Processor
{
    public interface IProcessor
    {
        void Process(HtmlPage page, ICrawlContext context);
    }
}