using System.Collections.Generic;
using DSharpPlus.EventArgs;
using MediatR;

namespace Nellebot.NotificationHandlers;

public abstract record EventNotification : INotification;

#pragma warning disable SA1402 // File may only contain a single type
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

public record SessionCreatedOrResumedNotification(string EventSource) : EventNotification;

public record ClientDisconnected(SocketCloseEventArgs EventArgs) : EventNotification;

public record BufferedMemberLeftNotification(IEnumerable<string> Usernames) : EventNotification;
