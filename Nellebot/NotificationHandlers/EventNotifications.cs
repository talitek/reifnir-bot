using DSharpPlus.EventArgs;
using MediatR;
using Nellebot.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

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

public record ErrorTestNotification(EventContext? Ctx, string Message, int Sleep) : EventNotification(Ctx);

public class ErrorTestNotificationHandler1 : INotificationHandler<ErrorTestNotification>
{
    public async Task Handle(ErrorTestNotification notification, CancellationToken cancellationToken)
    {
        if(notification.Ctx != null) await notification.Ctx.Channel.SendMessageAsync($"H1 Start: {notification.Message}");

        if (notification.Sleep > 0) await Task.Delay(notification.Sleep);

        if (notification.Ctx != null) await notification.Ctx.Channel.SendMessageAsync($"H1 Done: {notification.Message}");

        throw new Exception(notification.Message);
    }
}

public class ErrorTestNotificationHandler2 : INotificationHandler<ErrorTestNotification>
{
    public async Task Handle(ErrorTestNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Ctx != null) await notification.Ctx.Channel.SendMessageAsync($"H2 Start: {notification.Message}");

        if (notification.Sleep > 0) await Task.Delay(notification.Sleep);

        if (notification.Ctx != null) await notification.Ctx.Channel.SendMessageAsync($"H2 Done: {notification.Message}");

        throw new Exception(notification.Message);
    }
}
