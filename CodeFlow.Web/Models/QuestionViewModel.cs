using CodeFlow.core.Models;

namespace CodeFlow.Web.Models
{
    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; } = string.Empty;
        public string BodyMarkdown { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public int ViewCount { get; set; } = 0;
        public int Score { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public List<Tag> Tags { get; set; } = new List<Tag>();
        public bool IsAuthor { get; set; }
        public int CurrentVote { get; set; } = 0;
        public IEnumerable<Comment> Comments { get; set; } = [];

        public QuestionViewModel(Question question)
        {
            Id = question.Id; 
            Title = question.Title;
            Body = question.Body;
            BodyMarkdown = question.BodyMarkdown;
            BodyHtml = question.BodyHtml;
            ViewCount = question.ViewCount;
            Score = question.Score;
            CreatedAt = question.CreatedAt;
            UpdatedAt = question.UpdatedAt;
            UserId = question.UserId;
            User = question.User;
            Tags = question.Tags;
        }
    }
}
