using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nellebot.Attributes;

/// <summary>
///     Reject commands coming from DM or from other guild (if it exists).
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class BaseCommandCheckV2 : IContextCheck<BaseCommandCheckV2Attribute>
{
    public ValueTask<string?> ExecuteCheckAsync(BaseCommandCheckV2Attribute attribute, CommandContext ctx)
    {
        BotOptions botOptions = ctx.Client.ServiceProvider.GetRequiredService<IOptions<BotOptions>>().Value;

        ulong guildId = botOptions.GuildId;

        DiscordChannel channel = ctx.Channel;

        if (channel.IsPrivate)
            return ValueTask.FromResult<string?>("This command must be executed in a guild.");

        bool isHomeGuildChannel = channel.GuildId == guildId;

        return isHomeGuildChannel
            ? ValueTask.FromResult<string?>(null)
            : ValueTask.FromResult<string?>("Hmm. Weird.");
    }
}

public class BaseCommandCheckV2Attribute : ContextCheckAttribute;
