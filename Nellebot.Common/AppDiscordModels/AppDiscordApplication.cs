using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.AppDiscordModels
{
    public class AppDiscordApplication
    {
        public IEnumerable<AppDiscordUser> Owners { get; set; } = null!;
    }
}
