using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck, RequireOwnerOrAdmin]
    [Group("admin")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AdminModule : BaseCommandModule
    {
        private readonly ILogger<AdminModule> _logger;
        private readonly BotOptions _options;

        public AdminModule(
            ILogger<AdminModule> logger,
            IOptions<BotOptions> options)
        {
            _logger = logger;
            _options = options.Value;
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

        [Command("cookie-channels")]
        public async Task ListCookieChannels(CommandContext ctx)
        {
            var groupIds = _options.AwardVoteGroupIds;

            if (groupIds == null) return;

            var sb = new StringBuilder();

            var guildChannels = await ctx.Guild.GetChannelsAsync();

            var channelGroups = new List<Tuple<string, List<DiscordChannel>>>();

            var categoryChannels = guildChannels
                .Where(c => c.Type == ChannelType.Category
                            && groupIds.Contains(c.Id));

            foreach (var category in categoryChannels)
            {
                sb.AppendLine($"**{category.Name}**");

                var textChannelsForCategory = guildChannels.Where(c => c.Type == ChannelType.Text && c.ParentId == category.Id);

                foreach (var channel in textChannelsForCategory)
                {
                    sb.AppendLine($"#{channel.Name}");
                }

                sb.AppendLine();
            }

            await ctx.RespondAsync(sb.ToString());
        }
    }
}
