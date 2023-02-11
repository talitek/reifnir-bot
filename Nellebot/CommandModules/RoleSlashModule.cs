using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Options;
using Nellebot.Services;

namespace Nellebot.CommandModules;
public class RoleSlashModule : ApplicationCommandModule
{
    private const int _maxSelectComponentOptions = 25;
    private readonly RoleService _roleService;
    private readonly BotOptions _options;

    public RoleSlashModule(RoleService roleService, IOptions<BotOptions> options)
    {
        _roleService = roleService;
        _options = options.Value;
    }

    [SlashCommand("test", "Test")]
    public async Task TestSlashCommand(InteractionContext ctx)
    {
        // Respond with either CreateResponseAsync directly
        // Or DeferAsync + EditResponseAsync
        var aButton = new DiscordButtonComponent(ButtonStyle.Primary, "aButton", "Click me");

        var interactionBuilder = new DiscordInteractionResponseBuilder()
           .WithContent("I does da test")
           .AsEphemeral()
           .AddComponents(aButton);

        await ctx.CreateResponseAsync(interactionBuilder);

        // The original response is the bot's response to the command
        var originalResponse = await ctx.GetOriginalResponseAsync();

        /*
         *  await ctx.DeferAsync(true);
         *
         *  var webhookBuilder = new DiscordWebhookBuilder()
         *      .WithContent("I does da test")
         *      .AddComponents(aButton);
         *
         *  // When using the EditResponse method, there's no need to call getOriginalResponseAsync()
         *  var originalResponse = await ctx.EditResponseAsync(webhookBuilder);
        */

        // At this point, regardless of answering with Create or Defer + Edit
        // We're done with the Slash command interaction
        var interaction = await originalResponse.WaitForButtonAsync();

        var responseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent("Thanks for clicking me");

        await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, responseBuilder);
    }

    [SlashCommand("roles", "Chooz da rolez")]
    public Task Roles(InteractionContext ctx)
    {
        var member = ctx.Member ?? throw new Exception("Not a guild member");

        var hasMemberRole = member.Roles.Any(r => r.Id == _options.MemberRoleId);

        return hasMemberRole ? MemberRoleFlow(ctx) : NewUserRoleFlow(ctx);
    }

    /// <summary>
    /// Role chooser flow for new users.
    /// Present each role category input one after the other.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task NewUserRoleFlow(InteractionContext ctx)
    {
        // Acknowledge the command interaction by responding with a "thinking..." message
        await ctx.DeferAsync(true);

        var userRoles = await _roleService.GetRoleList();

        var roleGroups = userRoles
            .GroupBy(r => r.GroupNumber)
            .OrderBy(r => r.Key.HasValue ? r.Key : int.MaxValue);

        var currentInteraction = ctx.Interaction;

        foreach (var roleGroup in roleGroups.ToList())
        {
            // var isLastGroup = roleGroup == roleGroups.Last();
            var isSingleSelect = roleGroup.Key.HasValue;

            var roleDropdownOptions = new List<DiscordSelectComponentOption>();

            foreach (var userRole in roleGroup.ToList())
            {
                roleDropdownOptions.Add(new DiscordSelectComponentOption(userRole.Name, userRole.Id.ToString(), userRole.Name));
            }

            var maxOptions = isSingleSelect ? 1 : Math.Min(roleDropdownOptions.Count, _maxSelectComponentOptions);

            var roleDropdownId = "dropdown_role";
            var roleDropdownPlaceHolder = isSingleSelect ? $"Chooz a role for group {roleGroup.Key}" : "Chooz sum moar rolez";
            var roleDropdown = new DiscordSelectComponent(roleDropdownId, roleDropdownPlaceHolder, roleDropdownOptions, maxOptions: maxOptions);

            var interactionResultResponse = new DiscordWebhookBuilder().AddComponents(roleDropdown);

            // Respond by editing the deferred message
            var theMessage = await currentInteraction.EditOriginalResponseAsync(interactionResultResponse);

            // Wait for the user to select a role
            var interactionResult = await theMessage.WaitForSelectAsync(roleDropdownId);

            // Acknowledge the interaction.
            // This deferred message will be handled either in the next interation or outside the loop when we're done
            await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            // TODO: Store and handle this result
            // var result = interactionResult.Result.Values;

            // Set the interaction for the next iteration
            currentInteraction = interactionResult.Result.Interaction;
        }

        // Respond by editing the deferred message with a thank you message
        var responseBuilder = new DiscordWebhookBuilder().WithContent("Thank for choozin yo rolez");

        await currentInteraction.EditOriginalResponseAsync(responseBuilder);
    }

    /// <summary>
    /// Role chooser flow for existing users aka members
    /// Executing the command first prompts the user to choose
    /// the role category they want to choose roles from.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task MemberRoleFlow(InteractionContext ctx)
    {
        // Acknowledge the command interaction by responding with a "thinking..." message
        await ctx.DeferAsync(true);

        var userRoles = await _roleService.GetRoleList();

        var roleGroups = userRoles
            .GroupBy(r => r.GroupNumber)
            .OrderBy(r => r.Key.HasValue ? r.Key : int.MaxValue);

        var buttonRow = new List<DiscordComponent>();

        foreach (var roleGroup in roleGroups.ToList())
        {
            string buttonId = roleGroup.Key?.ToString() ?? "none";
            string buttonLabel = roleGroup.Key.HasValue ? $"Roles in group {roleGroup.Key + 1}" : "Ungrouped roles";

            buttonRow.Add(new DiscordButtonComponent(ButtonStyle.Primary, buttonId, buttonLabel));
        }

        var roleGroupButtonsResponse = new DiscordWebhookBuilder()
            .WithContent("Chooz watcha wanna chainge")
            .AddComponents(buttonRow);

        // Respond by editing the deferred message
        var theMessage = await ctx.EditResponseAsync(roleGroupButtonsResponse);

        var theMessageInteractivityResult = await theMessage.WaitForButtonAsync();

        // The button has been pressed. We need to either acknowledge or respond to the interaction right away.

        // Defer with a thinking message
        // await theMessageInteractivityResult.Result.Interaction.DeferAsync(true);
        // OR
        // Just acknowledge the interaction
        await theMessageInteractivityResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var chosenButtonId = theMessageInteractivityResult.Result.Id;

        var isSingleSelect = chosenButtonId != "none";

        var roleGroupToChange = isSingleSelect
            ? roleGroups.Single(g => g.Key.HasValue && g.Key.Value == uint.Parse(chosenButtonId))
            : roleGroups.Single(g => !g.Key.HasValue);

        var roleDropdownOptions = new List<DiscordSelectComponentOption>();

        foreach (var userRole in roleGroupToChange.ToList())
        {
            roleDropdownOptions.Add(new DiscordSelectComponentOption(userRole.Name, userRole.Id.ToString(), userRole.Name));
        }

        var maxOptions = isSingleSelect ? 1 : Math.Min(roleDropdownOptions.Count, _maxSelectComponentOptions);

        var roleDropdownId = "dropdown_role";
        var roleDropdownPlaceHolder = isSingleSelect ? "Chooz a role" : "Chooz sum rolez";
        var roleDropdown = new DiscordSelectComponent(roleDropdownId, roleDropdownPlaceHolder, roleDropdownOptions, maxOptions: maxOptions);

        var buttonInteractionResultResponse = new DiscordWebhookBuilder().AddComponents(roleDropdown);

        // Respond by editing the deferred message (a second time)
        theMessage = await theMessageInteractivityResult.Result.Interaction.EditOriginalResponseAsync(buttonInteractionResultResponse);

        var rolesMessageInteractivityResult = await theMessage.WaitForSelectAsync(roleDropdownId);

        // Just acknowledge interaction
        await rolesMessageInteractivityResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var choices = rolesMessageInteractivityResult.Result.Values;

        var dropdownInteractionResultResponse = new DiscordWebhookBuilder()
            .WithContent($"Yu choz diz/deez rolez: {string.Join(", ", choices)}");

        // Respond by editing the deferred message (a third time)
        _ = await rolesMessageInteractivityResult.Result.Interaction.EditOriginalResponseAsync(dropdownInteractionResultResponse);
    }
}
