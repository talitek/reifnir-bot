using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nellebot.Attributes;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;
using System;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [RequireOwnerOrAdmin]
    [Group("admin")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AdminModule : BaseCommandModule
    {
        private readonly ILogger<AdminModule> _logger;

        public AdminModule(
            ILogger<AdminModule> logger)
        {
            _logger = logger;
        }

        [Command("nickname")]
        public async Task ChangeNickname(CommandContext ctx, [RemainingText] string name)
        {
            name = name.RemoveQuotes();

            await ctx.Guild.CurrentMember.ModifyAsync((props) =>
            {
                props.Nickname = name;
            });
        }

        [Command("error-test")]
        public void ErrorTest(CommandContext ctx)
        {
            throw new Exception("Test error");
        }

        [Command("access-test")]
        public async Task AccessTest(CommandContext ctx)
        {
            await ctx.RespondAsync("Nice!");
        }
    }
}
