using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record ValhallKickUserCommand(CommandContext Ctx, DiscordMember Member, string Reason) : BotCommandCommand(Ctx);

public class ValhallKickUserHandler : IRequestHandler<ValhallKickUserCommand>
{
    private const int MaxAccountAgeForKickInDays = 7;
    private const int MaxGuildAgeForKickInDays = 1;

    public Task Handle(ValhallKickUserCommand request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var currentMember = ctx.Member ?? throw new Exception("Member is null");
        var targetMember = request.Member;

        if (ctx.Member?.Id == targetMember.Id)
            return ctx.RespondAsync("Hmm");

        var accountAge = DateTimeOffset.UtcNow - targetMember.CreationTimestamp;
        var guildAge = DateTimeOffset.UtcNow - targetMember.JoinedAt;

        if (accountAge.TotalDays >= MaxAccountAgeForKickInDays || guildAge.TotalDays >= MaxGuildAgeForKickInDays)
            return ctx.RespondAsync("At this juncture in the temporal continuum, the window of opportunity for rectifying or influencing the current circumstances has lamentably and irrevocably elapsed, rendering any further attempts to alter the outcome both futile and inconsequential.");

        var kickReason = $"Kicked on behalf of {currentMember.DisplayName}. Reason: {request.Reason.NullOrWhiteSpaceTo("/shrug")}";

        return targetMember.RemoveAsync(kickReason);
    }
}
