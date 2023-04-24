using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.Services
{
    public class AuthorizationService
    {
        private readonly BotOptions _options;

        public AuthorizationService(IOptions<BotOptions> options)
        {
            _options = options.Value;
        }

        public bool IsOwnerOrAdmin(AppDiscordMember member, AppDiscordApplication discordApplication)
        {
            var currentMember = member;

            var isBotOwner = discordApplication.Owners.Any(x => x.Id == currentMember.Id);

            if (isBotOwner)
                return true;

            var coOwnerUserId = _options.CoOwnerUserId;

            if (member.Id == coOwnerUserId)
                return true;

            var adminRoleId = _options.AdminRoleId;

            var currentUserIsAdmin = currentMember.Roles.Select(r => r.Id).Contains(adminRoleId);

            return currentUserIsAdmin;
        }

        public bool IsTrustedMember(AppDiscordMember appMember, AppDiscordApplication appApplication)
        {
            if (IsOwnerOrAdmin(appMember, appApplication))
                return true;

            var trustedRoleIds = _options.TrustedRoleIds;

            var currentUserIsTrusted = appMember.Roles.Select(r => r.Id).Intersect(trustedRoleIds).Any();

            return currentUserIsTrusted;
        }
    }
}
