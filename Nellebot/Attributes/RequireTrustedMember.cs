using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Nellebot.Common.AppDiscordModels;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;

namespace Nellebot.Attributes;

public class RequireTrustedMember : CheckBaseAttribute
{
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        AuthorizationService authorizationService = ctx.Services.GetService<AuthorizationService>()
                                                    ?? throw new Exception(
                                                        $"Could not resolve {nameof(AuthorizationService)}");

        if (ctx.Member == null) return Task.FromResult(false);

        AppDiscordMember appMember = DiscordMemberMapper.Map(ctx.Member);
        AppDiscordApplication appApplication = DiscordApplicationMapper.Map(ctx.Client.CurrentApplication);

        bool result = authorizationService.IsTrustedMember(appMember, appApplication);

        return Task.FromResult(result);
    }
}
