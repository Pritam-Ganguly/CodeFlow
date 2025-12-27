namespace CodeFlow.core.Servies
{
    public interface IMarkdownService
    {
        string Sanitize(string html);
        string ToHTML(string markdown);
    }
}