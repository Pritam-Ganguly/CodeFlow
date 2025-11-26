using CodeFlow.core.Models;

namespace CodeFlow.Web.Models
{
    public class HomeViewModel
    {
        public IEnumerable<Question> Questions { get; set; } = [];
        public string SearchTerm { get; set; } = string.Empty;
        public bool IsSearchResult { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

    }
}
