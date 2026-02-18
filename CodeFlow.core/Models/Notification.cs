namespace CodeFlow.core.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public int UserId { get; set; }
        public User? User { get; set; }
        public int QuestionId { get; set; }
        public Question? Question { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
