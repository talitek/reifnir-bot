using MediatR;
using Nellebot.Data.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class UntitledHandler : INotificationHandler<MessageCreatedNotification>
    {
        private readonly MessageRefRepository _messageRefRepo;

        public UntitledHandler(MessageRefRepository messageRefRepo)
        {
            _messageRefRepo = messageRefRepo;
        }

        public Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            return _messageRefRepo.CreateMessageRef(args.Message.Id, args.Channel.Id, args.Author.Id);
        }
    }
}
