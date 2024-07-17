using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record ValhallKickUserCommand(CommandContext Ctx, DiscordMember Member, string Reason)
    : BotCommandV2Command(Ctx);

// TODO: Consider removing the account age restriction
public class ValhallKickUserHandler : IRequestHandler<ValhallKickUserCommand>
{
    private const int MaxAccountAgeForKickInDays = 7;
    private const int MaxGuildAgeForKickInDays = 1;

    public async Task Handle(ValhallKickUserCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMember currentMember = ctx.Member ?? throw new Exception("Member is null");
        DiscordMember targetMember = request.Member;

        if (ctx.Member?.Id == targetMember.Id)
        {
            await ctx.RespondAsync("Hmm");
            return;
        }

        TimeSpan accountAge = DateTimeOffset.UtcNow - targetMember.CreationTimestamp;
        TimeSpan guildAge = DateTimeOffset.UtcNow - targetMember.JoinedAt;

        if (accountAge.TotalDays >= MaxAccountAgeForKickInDays || guildAge.TotalDays >= MaxGuildAgeForKickInDays)
        {
            await ctx.RespondAsync(
                "At this juncture in the temporal continuum, the window of opportunity for rectifying or influencing the current circumstances has lamentably and irrevocably elapsed, rendering any further attempts to alter the outcome both futile and inconsequential.");
            return;
        }

        var kickReason =
            $"Kicked on behalf of {currentMember.DisplayName}. Reason: {request.Reason.NullOrWhiteSpaceTo("/shrug")}";

        await targetMember.RemoveAsync(kickReason);
    }
}
