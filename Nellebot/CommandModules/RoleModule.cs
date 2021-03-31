using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class RoleModule: BaseCommandModule
    {
        private readonly UserRoleService _userRoleService;

        public RoleModule(UserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        [Command("role")]
        public async Task SetSelfRole(CommandContext ctx, string roleAlias)
        {
            throw new NotImplementedException();
        }
    }
}
