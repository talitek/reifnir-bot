using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;
using System;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [RequireOwner]
    [Group("admin")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AdminModule : BaseCommandModule
    {
        private readonly ILogger<AdminModule> _logger;
        private readonly GuildSettingsService _guildSettingsService;

        public AdminModule(
            ILogger<AdminModule> logger,
            GuildSettingsService guildSettingsService)
        {
            _logger = logger;
            _guildSettingsService = guildSettingsService;
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
            throw new Exception("Test error");
        }

        [Command("set-commands-channel")]
        public async Task SetCommandsChannel(CommandContext ctx, DiscordChannel channel)
        {
            var channelId = channel.Id;
            var guildId = channel.GuildId;

            await _guildSettingsService.SetBotChannel(guildId, BotChannelMap.Commands, channelId);

            await ctx.RespondAsync($"Set commands channel to #{channel.Name}");
        }

        [Command("set-log-channel")]
        public async Task SetLogChannelChannel(CommandContext ctx, DiscordChannel channel)
        {
            var channelId = channel.Id;
            var guildId = channel.GuildId;

            await _guildSettingsService.SetBotChannel(guildId, BotChannelMap.Log, channelId);

            await ctx.RespondAsync($"Set log channel to #{channel.Name}");
        }
    }
}
