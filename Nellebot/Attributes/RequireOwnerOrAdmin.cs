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
        //public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        //{
        //    var botOptionsService = ctx.Services.GetService(typeof(IOptions<BotOptions>));

        //    if (botOptionsService == null)
        //        throw new Exception("Could not fetch BotOptions service");

        //    var botOptions = ((IOptions<BotOptions>)botOptionsService).Value;

        //    var adminRoleId = botOptions.AdminRoleId;

        //    var currentMember = ctx.Member;

        //    var botApp = ctx.Client.CurrentApplication;

        //    var isBotOwner = botApp.Owners.Any(o => o.Id == currentMember.Id);

        //    if (isBotOwner)
        //        return Task.FromResult(true);

        //    var isAdmin = currentMember.Roles.Any(r => r.Id == adminRoleId);

        //    return Task.FromResult(isAdmin);
        //}

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var authorizationServiceObj = ctx.Services.GetService(typeof(AuthorizationService));

            if (authorizationServiceObj == null)
                throw new Exception("Could not fetch AuthorizationService");

            var authorizationService = (AuthorizationService)authorizationServiceObj;

            var appMember = DiscordMemberMapper.Map(ctx.Member);
            var appApplication = DiscordApplicationMapper.Map(ctx.Client.CurrentApplication);

            var result = authorizationService.IsOwnerOrAdmin(appMember, appApplication);

            return Task.FromResult(result);
        }
    }
}
