using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using MediatR;

namespace Nellebot.CommandHandlers;

public interface ICommand : IRequest
{ }

public interface IQuery : IRequest
{ }

public record BaseCommand : ICommand
{
    public BaseCommand(BaseContext ctx)
    {
        Ctx = ctx;
    }

    public BaseContext Ctx { get; init; }
}

public record MessageCommand : ICommand
{
    public MessageCommand(MessageContext ctx)
    {
        Ctx = ctx;
    }

    public MessageContext Ctx { get; init; }
}

public record BotCommandCommand : ICommand
{
    public BotCommandCommand(CommandContext ctx)
    {
        Ctx = ctx;
    }

    public CommandContext Ctx { get; init; }
}

public record InteractionCommand : ICommand
{
    public InteractionCommand(InteractionContext ctx)
    {
        Ctx = ctx;
    }

    public InteractionContext Ctx { get; init; }
}

public record BotCommandQuery : IQuery
{
    public BotCommandQuery(CommandContext ctx)
    {
        Ctx = ctx;
    }

    public CommandContext Ctx { get; init; }
}
