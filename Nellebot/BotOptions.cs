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

        /// <summary>
        /// Gets or sets discord user with access to admin commands.
        /// </summary>
        public ulong CoOwnerUserId { get; set; }

        /// <summary>
        /// Gets or sets discord role with access to admin commands.
        /// </summary>
        public ulong AdminRoleId { get; set; }

        public ulong ActivityLogChannelId { get; set; }

        public ulong ExtendedActivityLogChannelId { get; set; }

        public ulong GreetingsChannelId { get; set; }

        public ulong ErrorLogChannelId { get; set; }

        public ulong SuggestionsChannelId { get; set; }

        public ulong AwardChannelId { get; set; }

        public ulong[] AwardVoteGroupIds { get; set; } = null!;

        public int RequiredAwardCount { get; set; }

        public ulong MemberRoleId { get; set; }

        public ulong[] RequiredRoleIds { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether feature flag for populating message refs on Ready event.
        /// </summary>
        public bool AutoPopulateMessagesOnReadyEnabled { get; set; }

        public bool AutoCreateUserLogsEnabled { get; set; }

        public ulong ModmailChannelId { get; set; }

        public ulong? FakeDmChannelId { get; set; }
    }
}
