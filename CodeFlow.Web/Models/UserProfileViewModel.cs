using CodeFlow.core.Models;

namespace CodeFlow.Web.Models
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Reputation { get; set; }
        public UserProfile? UserProfile { get; set; }
        public IEnumerable<ReputationHistory> ReputationHistory { get; set; } = [];
        public IEnumerable<Badge> Badges { get; set; } = [];
        public IEnumerable<UserActivity> UserActivities { get; set; } = [];
        public IEnumerable<Question> Questions { get; set; } = [];
        public int QuestionsAsked => Questions.Count();
        public int AcceptedAnswerCount { get; set; }
    }
}
