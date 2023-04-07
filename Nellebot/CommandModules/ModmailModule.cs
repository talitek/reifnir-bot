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
        var introMessageContent = """
            Hello and welcome to Modmail! 
            Do you want to be a Chad and use your real (discord) name or be a Virgin and use a pseudonym?
            """;

        var realNameButton = new DiscordButtonComponent(ButtonStyle.Primary, "realNameButton", "Chad");
        var pseudonymButton = new DiscordButtonComponent(ButtonStyle.Primary, "pseudonymButton", "Virgin");

        var introMessageBuilder = new DiscordInteractionResponseBuilder()
           .WithContent(introMessageContent)
           .AddComponents(realNameButton, pseudonymButton);

        await ctx.CreateResponseAsync(introMessageBuilder);

        var originalResponse = await ctx.GetOriginalResponseAsync();

        var interactionResult = await originalResponse.WaitForButtonAsync();

        // Respond by removing the buttons from the original message
        introMessageBuilder.ClearComponents();
        await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, introMessageBuilder);

        var responseBuilder = new DiscordFollowupMessageBuilder();

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

        await ctx.FollowUpAsync(responseBuilder);
    }
}
