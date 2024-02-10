using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
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
    private readonly CommandParallelQueueChannel _commandParallelQueue;

    public GoodbyeMessageModule(CommandQueueChannel commandQueue, CommandParallelQueueChannel commandParallelQueue)
    {
        _commandQueue = commandQueue;
        _commandParallelQueue = commandParallelQueue;
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
        await _commandParallelQueue.Writer.WriteAsync(new GetGoodbyeMessagesCommand(ctx));
    }
}
