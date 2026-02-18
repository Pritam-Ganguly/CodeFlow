using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Models
{
    public class UserRole
    {
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public int RoleId { get; set; }
        public ApplicationRole? Role { get; set; }
    }
}
