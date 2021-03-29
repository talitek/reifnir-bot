using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    public class RandomModule : BaseCommandModule
    {
        [Command("oi")]
        public Task Oi(CommandContext ctx)
        {
            return ctx.RespondAsync("Oi!");
        }
    }

    
}
