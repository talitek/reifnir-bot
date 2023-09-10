using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models.UserLogs;
using Nellebot.Data.Repositories;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

public class ActivityLogHandler : INotificationHandler<GuildBanAddedNotification>,
                                        INotificationHandler<GuildBanRemovedNotification>,
                                        INotificationHandler<MessageDeletedNotification>,
                                        INotificationHandler<MessageBulkDeletedNotification>,
                                        INotificationHandler<GuildMemberAddedNotification>,
                                        INotificationHandler<GuildMemberRemovedNotification>,
                                        INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly DiscordLogger _discordLogger;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly MessageRefRepository _messageRefRepo;
    private readonly UserLogService _userLogService;
    private readonly BotOptions _botOptions;

    public ActivityLogHandler(DiscordLogger discordLogger, IDiscordErrorLogger discordErrorLogger, DiscordResolver discordResolver, MessageRefRepository messageRefRepo, UserLogService userLogService, IOptions<BotOptions> botOptions)
    {
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
        _messageRefRepo = messageRefRepo;
        _userLogService = userLogService;
        _botOptions = botOptions.Value;
    }

    public async Task Handle(GuildBanAddedNotification notification, CancellationToken cancellationToken)
    {
        GuildBanAddEventArgs args = notification.EventArgs;

        string memberName = args.Member.GetDetailedMemberIdentifier();

        DiscordAuditLogBanEntry? auditBanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>(
                                args.Guild, AuditLogActionType.Ban, (x) => x.Target.Id == args.Member.Id);

        if (auditBanEntry == null)
        {
            return;
        }

        DiscordMember? memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditBanEntry.UserResponsible.Id);

        if (memberResponsible is null)
        {
            return;
        }

        string responsibleName = memberResponsible.DisplayName;

        _discordLogger.LogActivityMessage($"**{memberName}** was banned by **{responsibleName}**. Reason: {auditBanEntry.Reason}.");
    }

    public async Task Handle(GuildBanRemovedNotification notification, CancellationToken cancellationToken)
    {
        GuildBanRemoveEventArgs args = notification.EventArgs;

        string memberName = args.Member.GetDetailedMemberIdentifier();

        DiscordAuditLogBanEntry? auditUnbanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>(
                                args.Guild, AuditLogActionType.Unban, (x) => x.Target.Id == args.Member.Id);

        if (auditUnbanEntry == null)
        {
            return;
        }

        DiscordMember? memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditUnbanEntry.UserResponsible.Id);

        if (memberResponsible is null)
        {
            return;
        }

        string responsibleName = memberResponsible.DisplayName;

        _discordLogger.LogActivityMessage($"**{memberName}** was unbanned by **{responsibleName}**.");
    }

    public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        MessageDeleteEventArgs args = notification.EventArgs;

        DiscordGuild guild = args.Guild;
        DiscordChannel channel = args.Channel;
        DiscordMessage deletedMessage = args.Message;

        if (channel.IsPrivate)
        {
            return;
        }

        if (channel.Id == _botOptions.ActivityLogChannelId || channel.Id == _botOptions.ExtendedActivityLogChannelId)
        {
            return;
        }

        AppDiscordMessage? message = await ResolveMessage(deletedMessage);

        if (message == null)
        {
            _discordErrorLogger.LogWarning($"{nameof(MessageDeletedNotification)}", $"Could not resolve message id {deletedMessage.Id}");

            _discordLogger.LogExtendedActivityMessage($"An unknown message in **{channel.Name}** was removed");

            return;
        }

        // the target is supposed to be a Message but the id corresponds to a user
        var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>(
                                        guild,
                                        AuditLogActionType.MessageDelete,
                                        (x) => x.Target.Id == message.Author.Id);

        // User deleted their own message
        if (!auditResolveResult.Resolved)
        {
            return;
        }

        DiscordAuditLogMessageEntry auditMessageDeleteEntry = auditResolveResult.Value;

        DiscordMember? authorAsMember = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

        DiscordMember? memberResponsible = await _discordResolver.ResolveGuildMember(guild, auditMessageDeleteEntry.UserResponsible.Id);

        string responsibleName = memberResponsible?.DisplayName ?? "Unknown mod";

        string authorName = authorAsMember?.GetDetailedMemberIdentifier() ?? "Unknown user";

        string logMessage = $"Message written by **{authorName}** in **{channel.Name}** was removed by **{responsibleName}**.";

        _discordLogger.LogActivityMessage(logMessage);

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            logMessage += $" Original message:{Environment.NewLine}> {message.Content}";
        }

        _discordLogger.LogExtendedActivityMessage(logMessage);
    }

    // TODO handle bulk deletion of messages belonging to different authors
    public async Task Handle(MessageBulkDeletedNotification notification, CancellationToken cancellationToken)
    {
        MessageBulkDeleteEventArgs args = notification.EventArgs;

        if (args.Messages.Count == 0)
        {
            return;
        }

        var messages = (await Task.WhenAll(args.Messages.Select(ResolveMessage))).ToList();

        DiscordUser? author = args.Messages.Where(m => m?.Author is not null).Select(m => m.Author).FirstOrDefault();

        if (author is null)
        {
            _discordErrorLogger.LogWarning($"{nameof(MessageBulkDeletedNotification)}", $"Could not find any message authors");

            _discordLogger.LogExtendedActivityMessage($"{messages.Count} unknown messages were removed.");

            return;
        }

        string authorName = author.GetDetailedUserIdentifier();

        var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>(
                                        args.Guild,
                                        AuditLogActionType.MessageDelete,
                                        (x) => x.Target.Id == author.Id);

        if (!auditResolveResult.Resolved)
        {
            return;
        }

        DiscordAuditLogMessageEntry auditMessageDeleteEntry = auditResolveResult.Value;

        DiscordMember? memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

        string responsibleName = memberResponsible?.DisplayName ?? "Unknown mod";

        _discordLogger.LogActivityMessage($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

        var sb = new StringBuilder();

        sb.AppendLine($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

        foreach (AppDiscordMessage message in messages.Where(x => x != null).Cast<AppDiscordMessage>())
        {
            sb.AppendLine();
            sb.AppendLine($"In {message.Channel.Name} at {message.CreationTimestamp}:");
            if (!string.IsNullOrWhiteSpace(message.Content))
            {
                sb.AppendLine($"> {message.Content}");
            }
        }

        _discordLogger.LogExtendedActivityMessage(sb.ToString());
    }

    public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
    {
        GuildMemberAddEventArgs args = notification.EventArgs;

        DiscordMember member = args.Member;

        string memberIdentifier = member.GetDetailedMemberIdentifier();

        _discordLogger.LogActivityMessage($"**{memberIdentifier}** joined the server");

        await _userLogService.CreateUserLog(member.Id, DateTime.UtcNow, UserLogType.JoinedServer);
        await _userLogService.CreateUserLog(member.Id, member.GetFullUsername(), UserLogType.UsernameChange);
        await _userLogService.CreateUserLog(member.Id, member.AvatarHash, UserLogType.AvatarHashChange);
    }

    public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
    {
        GuildMemberRemoveEventArgs args = notification.EventArgs;

        DiscordMember member = args.Member;
        DiscordGuild guild = args.Guild;

        string memberFullIdentifier = member.GetDetailedMemberIdentifier();

        // It's possible that the audit log entry might not be available right away.
        // If that turns out to be the case, consider wrapping this call into some sort of exeponential backoff retry.
        TryResolveResult<DiscordAuditLogKickEntry> auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogKickEntry>(
                                    args.Guild, AuditLogActionType.Kick, (x) => x.Target.Id == args.Member.Id);

        bool userWasKicked = auditResolveResult.Resolved;

        if (userWasKicked)
        {
            DiscordAuditLogKickEntry auditKickEntry = auditResolveResult.Value;

            DiscordMember? memberResponsible = await _discordResolver.ResolveGuildMember(guild, auditKickEntry.UserResponsible.Id);

            var kickReason = auditKickEntry.Reason.NullOrWhiteSpaceTo("*No reason provided*");

            if (memberResponsible is null)
            {
                return;
            }

            string responsibleName = memberResponsible.DisplayName;

            _discordLogger.LogActivityMessage($"**{memberFullIdentifier}** was kicked by **{responsibleName}**. Reason: {kickReason}.");

            await _userLogService.CreateUserLog(member.Id, DateTime.UtcNow, UserLogType.LeftServer, memberResponsible.Id);
        }
        else
        {
            _discordLogger.LogActivityMessage($"**{memberFullIdentifier}** left the server");

            await _userLogService.CreateUserLog(member.Id, DateTime.UtcNow, UserLogType.LeftServer);
        }
    }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        int totalChanges = 0;

        GuildMemberUpdateEventArgs args = notification.EventArgs;

        bool rolesUpdated = CheckForRolesUpdate(args);
        if (rolesUpdated)
        {
            totalChanges++;
        }

        bool nicknameUpdated = await CheckForNicknameUpdate(args);
        if (nicknameUpdated)
        {
            totalChanges++;
        }

        bool guildAvatarUpdated = await CheckForGuildAvatarUpdate(args);
        if (guildAvatarUpdated)
        {
            totalChanges++;
        }

        bool usernameUpdated = await CheckForUsernameUpdate(args);
        if (usernameUpdated)
        {
            totalChanges++;
        }

        bool avatarUpdated = await CheckForAvatarUpdate(args);
        if (avatarUpdated)
        {
            totalChanges++;
        }

        // Test if there actually are several changes in the same event
        if (totalChanges > 1)
        {
            _discordLogger.LogExtendedActivityMessage($"{nameof(GuildMemberUpdatedNotification)} contained more than 1 changes");
        }
    }

    private async Task<bool> CheckForAvatarUpdate(GuildMemberUpdateEventArgs args)
    {
        string? avatarAfter = args.MemberAfter.AvatarHash;
        string? avatarBefore = args.MemberBefore?.AvatarHash;

        if (string.IsNullOrWhiteSpace(avatarBefore) || avatarBefore == avatarAfter)
        {
            avatarBefore = (await _userLogService.GetLatestFieldForUser(args.Member.Id, UserLogType.AvatarHashChange))?.GetValue<string>();
        }

        if (avatarBefore == avatarAfter)
        {
            return false;
        }

        string message = $"Avatar change for {args.Member.Mention}. {avatarBefore ?? "*no avatar*"} => {avatarAfter ?? "*no avatar*"}.";

        _discordLogger.LogExtendedActivityMessage(message);

        await _userLogService.CreateUserLog(args.Member.Id, avatarAfter, UserLogType.AvatarHashChange);

        return true;
    }

    private async Task<bool> CheckForUsernameUpdate(GuildMemberUpdateEventArgs args)
    {
        string? usernameAfter = args.MemberAfter.GetFullUsername();
        string? usernameBefore = args.MemberBefore?.GetFullUsername();

        if (string.IsNullOrWhiteSpace(usernameBefore) || usernameBefore == usernameAfter)
        {
            usernameBefore = (await _userLogService.GetLatestFieldForUser(args.Member.Id, UserLogType.UsernameChange))?.GetValue<string>();
        }

        if (usernameBefore == usernameAfter)
        {
            return false;
        }

        _discordLogger.LogExtendedActivityMessage($"Username change for {args.Member.Mention}. {usernameBefore ?? "??"} => {usernameAfter ?? "??"}.");

        await _userLogService.CreateUserLog(args.Member.Id, usernameAfter, UserLogType.UsernameChange);

        return true;
    }

    private async Task<bool> CheckForGuildAvatarUpdate(GuildMemberUpdateEventArgs args)
    {
        string? guildAvatarHashAfter = args.GuildAvatarHashAfter;
        string? guildAvatarHashBefore = args.GuildAvatarHashAfter;

        if (string.IsNullOrWhiteSpace(guildAvatarHashBefore) || guildAvatarHashBefore == guildAvatarHashAfter)
        {
            guildAvatarHashBefore = (await _userLogService.GetLatestFieldForUser(args.Member.Id, UserLogType.GuildAvatarHashChange))?.GetValue<string>();
        }

        if (guildAvatarHashBefore == guildAvatarHashAfter)
        {
            return false;
        }

        string message = $"Guild avatar change for {args.Member.Mention}. {guildAvatarHashBefore ?? "*no avatar*"} => {guildAvatarHashAfter ?? "*no avatar*"}.";

        _discordLogger.LogExtendedActivityMessage(message);

        await _userLogService.CreateUserLog(args.Member.Id, guildAvatarHashAfter, UserLogType.GuildAvatarHashChange);

        return true;
    }

    private async Task<bool> CheckForNicknameUpdate(GuildMemberUpdateEventArgs args)
    {
        string? nicknameAfter = args.NicknameAfter;
        string? nicknameBefore = args.NicknameBefore;

        if (string.IsNullOrWhiteSpace(nicknameBefore) || nicknameBefore == nicknameAfter)
        {
            nicknameBefore = (await _userLogService.GetLatestFieldForUser(args.Member.Id, UserLogType.NicknameChange))?.GetValue<string>();
        }

        // TODO check if member's nickname was changed by moderator
        if (nicknameBefore == nicknameAfter)
        {
            return false;
        }

        string message = $"Nickname change for {args.Member.Mention}. {nicknameBefore ?? "*no nickname*"} => {nicknameAfter ?? "*no nickname*"}.";

        _discordLogger.LogExtendedActivityMessage(message);

        await _userLogService.CreateUserLog(args.Member.Id, nicknameAfter, UserLogType.NicknameChange);

        return true;
    }

    private bool CheckForRolesUpdate(GuildMemberUpdateEventArgs args)
    {
        DiscordRole? addedRole = args.RolesAfter.ExceptBy(args.RolesBefore.Select(r => r.Id), x => x.Id).FirstOrDefault();
        DiscordRole? removedRole = args.RolesBefore.ExceptBy(args.RolesAfter.Select(r => r.Id), x => x.Id).FirstOrDefault();

        string memberMention = args.Member.Mention;
        string memberDisplayName = args.Member.DisplayName;

        if (addedRole is not null)
        {
            _discordLogger.LogActivityMessage($"Added role **{addedRole.Name}** to **{memberDisplayName}**");
            _discordLogger.LogExtendedActivityMessage($"Role change for {memberMention}: Added {addedRole.Name}.");
            return true;
        }

        if (removedRole is not null)
        {
            _discordLogger.LogActivityMessage($"Removed role **{removedRole.Name}** from **{memberDisplayName}**");
            _discordLogger.LogExtendedActivityMessage($"Role change for {memberMention}: Removed {removedRole.Name}.");
            return true;
        }

        return false;
    }

    private async Task<AppDiscordMessage?> ResolveMessage(DiscordMessage deletedMessage)
    {
        if (deletedMessage is null)
        {
            return null;
        }

        AppDiscordMessage appDiscordMessage = DiscordMessageMapper.Map(deletedMessage);

        bool isCompletedMessage = !string.IsNullOrWhiteSpace(appDiscordMessage.Content) && appDiscordMessage.Author != null;

        if (isCompletedMessage)
        {
            return appDiscordMessage;
        }

        Common.Models.MessageRef? messageRef = await _messageRefRepo.GetMessageRef(deletedMessage.Id);

        if (messageRef == null)
        {
            return null;
        }

        appDiscordMessage.Author = new AppDiscordMember() { Id = messageRef.UserId };

        return appDiscordMessage;
    }
}
