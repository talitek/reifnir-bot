using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.MessageTemplates;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[RequireTrustedMember]
[Group("goodbye-msg")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class GoodbyeMessageModule : BaseCommandModule
{
    private readonly CommandQueueChannel _commandQueue;
    private readonly BotOptions _options;

    public GoodbyeMessageModule(CommandQueueChannel commandQueue, IOptions<BotOptions> options)
    {
        _commandQueue = commandQueue;
        _options = options.Value;
    }

    [Command("add")]
    public async Task AddGoodbyeMessage(CommandContext ctx, [RemainingText] string message)
    {
        await _commandQueue.Writer.WriteAsync(new AddGoodbyeMessageCommand(ctx, message));
    }

    [Command("remove")]
    public async Task RemoveGoodbyeMessage(CommandContext ctx, [RemainingText] string messageId)
    {
        await _commandQueue.Writer.WriteAsync(new DeleteGoodbyeMessageCommand(ctx, messageId));
    }

    [Command("list")]
    public async Task GetGoodbyeMessages(CommandContext ctx)
    {
        await _commandQueue.Writer.WriteAsync(new GetGoodbyeMessagesCommand(ctx));
    }
}
