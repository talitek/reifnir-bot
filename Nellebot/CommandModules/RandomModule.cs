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

        [Command("ban")]
        public Task Ban(CommandContext ctx) => ctx.RespondAsync("Do it yourself!");
    }

    
}
