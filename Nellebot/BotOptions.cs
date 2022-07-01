using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot
{
    public class BotOptions
    {
        public const string OptionsKey = "Nellebot";

        public string CommandPrefix { get; set; } = null!;
        public string ConnectionString { get; set; } = null!;
        public string BotToken { get; set; } = null!;
        public string OrdbokApiKey { get; set; } = null!;
        public ulong GuildId { get; set; }

        public ulong LogChannelId { get; set; }
        public ulong AuditLogChannelId { get; set; }
        public ulong GreetingsChannelId { get; set; }
        public ulong ErrorLogChannelId { get; set; }
        public ulong SuggestionsChannelId { get; set; }
        public ulong AwardChannelId { get; set; }
        public ulong[] AwardVoteGroupIds { get; set; } = null!;

        public int RequiredAwardCount { get; set; }

        public ulong AdminRoleId { get; set; }
        public ulong MemberRoleId { get; set; }
        public ulong[] RequiredRoleIds { get; set; } = null!;
    }
}

