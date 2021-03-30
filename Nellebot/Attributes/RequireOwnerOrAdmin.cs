using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Options;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Attributes
{
    public class RequireOwnerOrAdmin : CheckBaseAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var authorizationServiceObj = ctx.Services.GetService(typeof(AuthorizationService));

            if (authorizationServiceObj == null) {
                var error = "Could not fetch AuthorizationService";

                var discordErrorLoggerObj = ctx.Services.GetService(typeof(DiscordErrorLogger));

                if(discordErrorLoggerObj == null)
                {
                    throw new Exception("Could not fetch DiscordErrorLogger");
                }

                var discordErrorLogger = (DiscordErrorLogger)discordErrorLoggerObj;

                await discordErrorLogger.LogDiscordError(error);

                return false;
            }

            var authorizationService = (AuthorizationService)authorizationServiceObj;

            var appMember = DiscordMemberMapper.Map(ctx.Member);
            var appApplication = DiscordApplicationMapper.Map(ctx.Client.CurrentApplication);

            var result = authorizationService.IsOwnerOrAdmin(appMember, appApplication);

            return result;
        }
    }
}
