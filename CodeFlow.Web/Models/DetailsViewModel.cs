namespace CodeFlow.Web.Models
{
    public class DetailsViewModel
    {
        public int TotalAnswers { get; set; }
        public QuestionViewModel? Question { get; set; }
        public IEnumerable<AnswerViewModel> Answers { get; set; } = [];

    }
}
