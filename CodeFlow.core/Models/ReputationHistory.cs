using CodeFlow.core.Models.Mapping;

namespace CodeFlow.core.Models
{
    public class ReputationHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int Amount { get; set; }
        public ReputationTransactionTypes TransactionType { get; set; }
        public int RelatedPostId { get; set; }
        public RelatedPostType RelatedPostType { get; set; }
        public int? ActingUserId { get; set; }
        public User? ActingUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;

    }
}
