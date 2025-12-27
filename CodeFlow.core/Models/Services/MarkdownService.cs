using AngleSharp.Html.Dom;
using Ganss.Xss;
using Markdig;

namespace CodeFlow.core.Servies
{
    public class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _markdownPipeline;
        private readonly HtmlSanitizer _htmlSanitizer;

        public MarkdownService()
        {
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UsePipeTables()
                .UseAutoLinks()
                .UseEmojiAndSmiley()
                .UseMathematics()
                .Build();

            _htmlSanitizer = new HtmlSanitizer();
            _htmlSanitizer.AllowedSchemes.Add("mailto");

            _htmlSanitizer.PostProcessNode += (sender, e) =>
            {
                if (e.Node is IHtmlAnchorElement a)
                {
                    a.RelationList.Remove();
                    a.RelationList.Add("external");
                    a.RelationList.Add("nofollow");
                    a.SetAttribute("target", "_blank");
                }
            };
        }

        /// <summary>
        /// Method to convert markdown to sanitized HTML
        /// </summary>
        /// <param name="markdown">Markdown value</param>
        /// <returns>HTML version of markdown</returns>
        public string ToHTML(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return string.Empty;
            }

            var html = Markdown.ToHtml(markdown, _markdownPipeline);
            return Sanitize(html);
        }

        /// <summary>
        /// Method to sanitize HTML
        /// </summary>
        /// <param name="html">HTML input</param>
        /// <returns>Sanitized HTML value</returns>
        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            return _htmlSanitizer.Sanitize(html, "https://google.com");
        }

    }
}
