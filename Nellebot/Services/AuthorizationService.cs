using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Common.AppDiscordModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class AuthorizationService
    {
        private readonly BotOptions _options;

        public AuthorizationService(
            IOptions<BotOptions> options
            )
        {
            _options = options.Value;
        }

        public bool IsOwnerOrAdmin(AppDiscordMember member, AppDiscordApplication discordApplication)
        {
            var adminRoleId = _options.AdminRoleId;

            var currentMember = member;

            var botApp = discordApplication;

            var isBotOwner = botApp.Owners.Any(x => x.Id == currentMember.Id);

            if (isBotOwner)
                return true;

            var currentUserIsAdmin = currentMember.Roles.Select(r => r.Id).Contains(adminRoleId);

            return currentUserIsAdmin;
        }
    }
}
