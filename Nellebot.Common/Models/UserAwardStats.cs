using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models
{
    public class UserAwardStats
    {
        public uint TotalAwardCount { get; set; }
        public uint AwardMessageCount { get; set; }
        public List<AwardMessage> TopAwardedMessages { get; set; } = null!;        
    }
}
