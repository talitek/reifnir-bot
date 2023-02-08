using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;
using Nellebot.Services.Loggers;

namespace Nellebot.Attributes;

public class RequireOwnerOrAdmin : CheckBaseAttribute
{
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        object? authorizationServiceObj = ctx.Services.GetService(typeof(AuthorizationService));

        if (authorizationServiceObj == null)
        {
            string error = "Could not fetch AuthorizationService";

            object? discordErrorLoggerObj = ctx.Services.GetService(typeof(IDiscordErrorLogger));

            if (discordErrorLoggerObj == null)
            {
                throw new Exception("Could not fetch DiscordErrorLogger");
            }

            var discordErrorLogger = (IDiscordErrorLogger)discordErrorLoggerObj;

            discordErrorLogger.LogError("WTF", error);

            return Task.FromResult(false);
        }

        var authorizationService = (AuthorizationService)authorizationServiceObj;

        if (ctx.Member == null)
        {
            return Task.FromResult(false);
        }

        Common.AppDiscordModels.AppDiscordMember appMember = DiscordMemberMapper.Map(ctx.Member);
        Common.AppDiscordModels.AppDiscordApplication appApplication = DiscordApplicationMapper.Map(ctx.Client.CurrentApplication);

        bool result = authorizationService.IsOwnerOrAdmin(appMember, appApplication);

        return Task.FromResult(result);
    }
}
