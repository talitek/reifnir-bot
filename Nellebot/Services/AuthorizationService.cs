using System.Linq;
using Microsoft.Extensions.Options;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.Services;

public class AuthorizationService
{
    private readonly BotOptions _options;

    public AuthorizationService(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    public bool IsOwnerOrAdmin(AppDiscordMember member, AppDiscordApplication discordApplication)
    {
        AppDiscordMember currentMember = member;

        bool isBotOwner = discordApplication.Owners?.Any(x => x.Id == currentMember.Id) ?? false;

        if (isBotOwner)
        {
            return true;
        }

        ulong coOwnerUserId = _options.CoOwnerUserId;

        if (member.Id == coOwnerUserId)
        {
            return true;
        }

        ulong adminRoleId = _options.AdminRoleId;

        bool currentUserIsAdmin = currentMember.Roles.Select(r => r.Id).Contains(adminRoleId);

        return currentUserIsAdmin;
    }

    public bool IsTrustedMember(AppDiscordMember appMember, AppDiscordApplication appApplication)
    {
        if (IsOwnerOrAdmin(appMember, appApplication))
        {
            return true;
        }

        ulong[] trustedRoleIds = _options.TrustedRoleIds;

        bool currentUserIsTrusted = appMember.Roles.Select(r => r.Id).Intersect(trustedRoleIds).Any();

        return currentUserIsTrusted;
    }
}
