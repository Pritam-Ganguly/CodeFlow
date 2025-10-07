using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public int VoteType { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int? Questionid { get; set; }
        public Question? Question { get; set; }
        public int? AnswerId { get; set; }
        public Answer? Answer { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
