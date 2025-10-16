using CodeFlow.core.Models;

namespace CodeFlow.Web.Models
{
    public class AnswerViewModel
    {
        public int Id { get; set; }
        public string Body { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int QuestionId { get; set; }
        public Question? Question { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public bool IsAuthor { get; set; }
        public int CurrentVote { get; set; } = 0;
        public IEnumerable<Comment> Comments { get; set; } = [];

        public AnswerViewModel(Answer answer)
        {
            Id = answer.Id;
            Body = answer.Body;
            Score = answer.Score;
            IsAccepted = answer.IsAccepted;
            CreatedAt = answer.CreatedAt;
            UpdatedAt = answer.UpdatedAt;
            QuestionId = answer.QuestionId;
            Question = answer.Question;
            UserId = answer.UserId;
            User = answer.User;
        }
    }
}
