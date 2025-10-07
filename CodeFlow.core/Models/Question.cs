using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int ViewCount { get; set; } = 0;
        public int Score { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } 
        public int UserId { get; set; }
        public User? User { get; set; }
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}
