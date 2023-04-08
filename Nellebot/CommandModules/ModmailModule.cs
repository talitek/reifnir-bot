using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Options;
using Nellebot.Utils;

namespace Nellebot.CommandModules;

public class ModmailModule : ApplicationCommandModule
{
    private readonly BotOptions _options;

    public ModmailModule(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    [SlashCommand("modmail", "Send a message via the modmail")]
    public async Task OpenModmailTicket(InteractionContext ctx)
    {
        var isDmChannel = ctx.Channel.IsPrivate;

#if DEBUG
        isDmChannel |= ctx.Channel.Id == _options.FakeDmChannelId;
#endif

        var introMessageContent = """
            Hello and welcome to Modmail! 
            Do you want to be a Chad and use your real (discord) name or be a Virgin and use a pseudonym?
            """;

        var realNameButton = new DiscordButtonComponent(ButtonStyle.Primary, "realNameButton", "Chad");
        var pseudonymButton = new DiscordButtonComponent(ButtonStyle.Primary, "pseudonymButton", "Virgin");

        IDiscordMessageBuilder introMessageBuilder;
        DiscordMessage introMessage;

        if (isDmChannel)
        {
            introMessageBuilder = new DiscordInteractionResponseBuilder()
               .WithContent(introMessageContent)
               .AddComponents(realNameButton, pseudonymButton);

            await ctx.CreateResponseAsync(introMessageBuilder as DiscordInteractionResponseBuilder);

            introMessage = await ctx.GetOriginalResponseAsync();
        }
        else
        {
            var nonDmChannelResponseMessageContent = "I'll just slip into your DMs";
            await ctx.CreateResponseAsync(nonDmChannelResponseMessageContent);

            introMessageBuilder = new DiscordMessageBuilder()
                .WithContent(introMessageContent)
                .AddComponents(realNameButton, pseudonymButton);

#if DEBUG
            introMessage = await ctx.Guild.Channels[_options.FakeDmChannelId!.Value].SendMessageAsync(introMessageBuilder as DiscordMessageBuilder);
#else
            introMessage = await ctx.Member.SendMessageAsync(introMessageBuilder as DiscordMessageBuilder);
#endif

        }

        var interactionResult = await introMessage.WaitForButtonAsync();

        // Respond by removing the buttons from the original message
        // introMessageBuilder.ClearComponents();

        var choiceInteractionResponseBuilder = new DiscordInteractionResponseBuilder(introMessageBuilder);
        choiceInteractionResponseBuilder.ClearComponents();

        await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, choiceInteractionResponseBuilder);

        //if(introMessageBuilder is DiscordInteractionResponseBuilder introMessageInteractionResponseBuilder)
        //{
        //    await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, introMessageInteractionResponseBuilder);
        //} else
        //{

        //}


        var responseBuilder = new DiscordMessageBuilder();

        if (interactionResult.Result.Id == "realNameButton")
        {
            var nickname = ctx.Member?.GetNicknameOrDisplayName() ?? ctx.User.GetFullUsername();

            responseBuilder = responseBuilder.WithContent($"Wow, **{nickname}**. You're a real chad!");
        }
        else
        {
            var nickname = PseudonymGenerator.GetRandomPseudonym();

            responseBuilder = responseBuilder.WithContent($"Wow, **{nickname}**. You're a real virgin!");
        }

        var dmChannel = introMessage.Channel;

#if DEBUG
        dmChannel = ctx.Guild.Channels[_options.FakeDmChannelId!.Value];
#endif

        await dmChannel.SendMessageAsync(responseBuilder);
    }
}
