using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Services;

namespace Nellebot.CommandHandlers.MessageTemplates;

public record AddGoodbyeMessageCommand(CommandContext Ctx, string Message) : BotCommandCommand(Ctx);

public class AddGoodbyeMessageHandler : IRequestHandler<AddGoodbyeMessageCommand>
{
    private const int MaxMessageLength = 256;
    private const string UserToken = "$USER";
    private const int MaxUserTokensPerMessage = 3;
    private const string GoodbyeMessageTemplateType = "goodbye";
    private static readonly Regex UserTokenRegex = new(@"\$USER");

    private readonly MessageTemplateRepository _messageTemplateRepo;
    private readonly SharedCache _cache;

    public AddGoodbyeMessageHandler(MessageTemplateRepository messageTemplateRepo, SharedCache cache)
    {
        _messageTemplateRepo = messageTemplateRepo;
        _cache = cache;
    }

    public async Task Handle(AddGoodbyeMessageCommand request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var message = request.Message;
        var author = ctx.Member ?? throw new Exception("Member was null");

        try
        {
            if (message.Length > MaxMessageLength)
                throw new ArgumentException($"Message too long (unlike your pp). Max {MaxMessageLength} characters.");

            var userTokenCount = UserTokenRegex.Matches(message).Count;

            if (userTokenCount > MaxUserTokensPerMessage)
                throw new ArgumentException($"Too many {UserToken} tokens. Max {MaxUserTokensPerMessage} per message.");

            if (userTokenCount == 0)
                throw new ArgumentException($"Message must contain at least one {UserToken} token.");

            var messageTemplateWithBoldedUserToken = message.Replace(UserToken, $"**{UserToken}**");

            await _messageTemplateRepo.CreateMessageTemplate(messageTemplateWithBoldedUserToken, GoodbyeMessageTemplateType, author.Id, cancellationToken);

            _cache.FlushCache(SharedCacheKeys.GoodbyeMessages);

            var previewMemberMention = author.DisplayName;

            var messagePreview = message.Replace(UserToken, previewMemberMention);

            var sb = new StringBuilder("Goodbye message created successfully. Here's a preview:");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(messagePreview);

            await ctx.RespondAsync(sb.ToString());
        }
        catch (ArgumentException ex)
        {
            await ctx.RespondAsync(ex.Message);
        }
    }
}
