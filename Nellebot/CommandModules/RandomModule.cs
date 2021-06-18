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
        public Task Ban(CommandContext ctx, [RemainingText] string str)
        {
            return ctx.RespondAsync("<:katt_trist:622144052455800832> y u so mean? <:katt_trist:622144052455800832>");
        }
    }

    
}
