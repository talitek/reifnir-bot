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
    [BaseCommandCheck, RequireOwnerOrAdmin]
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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task ErrorTest(CommandContext ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //await ctx.RespondAsync("Error test command executed");

            throw new Exception("Test error");
        }

        [Command("access-test")]
        public async Task AccessTest(CommandContext ctx)
        {
            await ctx.RespondAsync("Nice!");
        }

        [Command("say")]
        public async Task AccessTest(CommandContext ctx, string message)
        {
            await ctx.RespondAsync(message);
        }
    }
}
