using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.AppDiscordModels
{
    public class AppDiscordMember
    {
        public ulong Id { get; set; }
        public IEnumerable<AppDiscordRole> Roles { get; set; } = null!;
    }
}
