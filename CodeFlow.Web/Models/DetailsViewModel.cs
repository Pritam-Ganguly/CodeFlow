namespace CodeFlow.Web.Models
{
    public class DetailsViewModel
    {
        public QuestionViewModel Question { get; set; }
        public IEnumerable<AnswerViewModel> Answers { get; set; } = [];

    }
}
