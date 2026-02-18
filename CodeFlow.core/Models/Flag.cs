using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Models
{
    public class Flag
    {
        public int Id { get; set; }
        public int FlagTypeId { get; set; }
        public FlagType? FlagType { get; set; }
        public FlagPostType PostType { get; set; } // "Question", "Answer", "Comment"
        public int PostId { get; set; }
        public int ReportingUserId { get; set; }
        public User? ReportingUser { get; set; }
        public string? Reason { get; set; }
        public FlagStatusType Status { get; set; } // Pending, UnderReview, Resolved, Dismissed
        public string? ResolutionNotes { get; set; }
        public int? ResolvedByUserId { get; set; }
        public User? ResolvedByUser { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FlagPostType
    {
        Question,
        Answer,
        Comment
    }

    public enum FlagStatusType
    {
        Pending,
        UnderReview,
        Resolved,
        Dismissed
    }
}
