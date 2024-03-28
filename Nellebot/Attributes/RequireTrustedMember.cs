using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;

namespace Nellebot.Attributes;

public class RequireTrustedMember : CheckBaseAttribute
{
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        var authorizationService = ctx.Services.GetService<AuthorizationService>()
                                   ?? throw new Exception($"Could not resolve {nameof(AuthorizationService)}");

        if (ctx.Member == null) return Task.FromResult(false);

        var appMember = DiscordMemberMapper.Map(ctx.Member);
        var appApplication = DiscordApplicationMapper.Map(ctx.Client.CurrentApplication);

        var result = authorizationService.IsTrustedMember(appMember, appApplication);

        return Task.FromResult(result);
    }
}
