using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFlow.core.Models
{
    public class FlagType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SeverityLevel { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }


}
