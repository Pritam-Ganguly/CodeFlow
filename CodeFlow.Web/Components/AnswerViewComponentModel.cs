using CodeFlow.Web.Models;

namespace CodeFlow.Web.Components
{
    public class AnswerViewComponentModel
    {
        public IEnumerable<AnswerViewModel> Answers { get; set; } = [];
        public bool IsAuthor { get; set; }
    }
}
