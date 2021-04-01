using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using System;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    public class RandomModule : BaseCommandModule
    {
        [Command("oi")]
        public Task Oi(CommandContext ctx)
        {
            return ctx.RespondAsync("Oi!");
        }
    }

    
}
