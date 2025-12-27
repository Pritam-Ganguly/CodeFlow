namespace CodeFlow.core.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public string BodyMarkdown { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int QuestionId { get; set; }
        public Question? Question { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; } 
    }
}
