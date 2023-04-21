using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

public class ModmailModule : ApplicationCommandModule
{
    private readonly CommandQueueChannel _channel;

    public ModmailModule(CommandQueueChannel channel)
    {
        _channel = channel;
    }

    [SlashCommand("modmail", "Send a message via the modmail")]
    public async Task RequestModmailTicket(InteractionContext ctx)
    {
        var messageContent = "I'll just slip into your DMs";

        var responseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent(messageContent)
            .AsEphemeral();

        await ctx.CreateResponseAsync(responseBuilder);

        var command = new RequestModmailTicketCommand(CommandHandlers.BaseContext.FromInteractionContext(ctx));

        await _channel.Writer.WriteAsync(command);
    }
}
