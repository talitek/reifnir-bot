using DSharpPlus.EventArgs;
using MediatR;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

// TODO make non nullable
public abstract record EventNotification(EventContext? Ctx = null) : INotification;

public record GuildMemberUpdatedNotification(GuildMemberUpdateEventArgs EventArgs) : EventNotification;
public record GuildMemberAddedNotification(GuildMemberAddEventArgs EventArgs) : EventNotification;
public record GuildMemberRemovedNotification(GuildMemberRemoveEventArgs EventArgs) : EventNotification;
public record GuildBanAddedNotification(GuildBanAddEventArgs EventArgs) : EventNotification;
public record GuildBanRemovedNotification(GuildBanRemoveEventArgs EventArgs) : EventNotification;

public record MessageCreatedNotification(MessageCreateEventArgs EventArgs) : EventNotification;
public record MessageDeletedNotification(MessageDeleteEventArgs EventArgs) : EventNotification;
public record MessageBulkDeletedNotification(MessageBulkDeleteEventArgs EventArgs) : EventNotification;
public record PresenceUpdatedNotification(PresenceUpdateEventArgs EventArgs) : EventNotification;

public record ClientHeartbeatNotification(HeartbeatEventArgs EventArgs) : EventNotification;
public record ClientReadyOrResumedNotification() : EventNotification();