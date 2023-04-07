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
using Nellebot.Services.Loggers;

namespace Nellebot.CommandModules;

public class RoleSlashModule : ApplicationCommandModule
{
    private const int MaxSelectComponentOptions = 25;
    private readonly RoleService _roleService;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly BotOptions _options;

    public RoleSlashModule(RoleService roleService, IOptions<BotOptions> options, IDiscordErrorLogger discordErrorLogger)
    {
        _roleService = roleService;
        _discordErrorLogger = discordErrorLogger;
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
        var interactionResult = await originalResponse.WaitForButtonAsync();

        var responseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent("Thanks for clicking me");

        await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, responseBuilder);
    }

    [SlashCommand("roles", "Choose your server roles")]
    public Task Roles(InteractionContext ctx)
    {
        try
        {
            var member = ctx.Member ?? throw new Exception("Not a guild member");

            var hasMemberRole = member.Roles.Any(r => r.Id == _options.MemberRoleId);

            return hasMemberRole ? MemberRoleFlow(ctx) : NewUserRoleFlow(ctx);
        }
        catch (Exception ex)
        {
            // TODO add better error handling for Slash commands
            _discordErrorLogger.LogError(ex, ex.Message);
            return Task.CompletedTask;
        }
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

        var user = ctx.Member;

        var userRoles = await _roleService.GetRoleList();

        var roleGroups = userRoles
            .GroupBy(r => r.Group)
            .OrderBy(r => r.Key?.Id ?? int.MaxValue);

        var currentInteraction = ctx.Interaction;

        var rolesToAdd = new List<ulong>();
        var rolesToRemove = new List<ulong>();

        foreach (var roleGroup in roleGroups.ToList())
        {
            var isSingleSelect = roleGroup.Key != null && roleGroup.Key.MutuallyExclusive;

            var roleDropdownOptions = new List<DiscordSelectComponentOption>();

            foreach (var userRole in roleGroup.ToList())
            {
                var userHasRole = user.Roles.Select(r => r.Id).Contains(userRole.RoleId);

                roleDropdownOptions.Add(new DiscordSelectComponentOption(userRole.Name, userRole.RoleId.ToString(), userRole.Name, isDefault: userHasRole));
            }

            var maxOptions = isSingleSelect ? 1 : Math.Min(roleDropdownOptions.Count, MaxSelectComponentOptions);
            var minOptions = isSingleSelect ? 1 : 0;

            var roleDropdownId = "dropdown_role";
            var roleDropdownPlaceHolder = isSingleSelect ? $"Choose 1 role" : "Choose any roles";
            var roleDropdown = new DiscordSelectComponent(roleDropdownId, roleDropdownPlaceHolder, roleDropdownOptions, minOptions: minOptions, maxOptions: maxOptions);

            var interactionResultResponse = new DiscordWebhookBuilder()
                .WithContent(roleGroup.Key != null ? $"{roleGroup.Key.Name} roles" : "Ungrouped roles")
                .AddComponents(roleDropdown);

            // Respond by editing the deferred message
            var theMessage = await currentInteraction.EditOriginalResponseAsync(interactionResultResponse);

            // Wait for the user to select a role
            var interactionResult = await theMessage.WaitForSelectAsync(roleDropdownId);

            // Acknowledge the interaction.
            // This deferred message will be handled either in the next interation or outside the loop when we're done
            await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            var chosenRoleIds = interactionResult.Result.Values.Select(ulong.Parse).ToList();

            var rolesFromGroupToAdd = chosenRoleIds
                .Except(user.Roles.Select(r => r.Id))
                .ToList();

            rolesToAdd.AddRange(rolesFromGroupToAdd);

            var rolesFromGroupToRemove = user.Roles.Select(r => r.Id)
                .Intersect(roleGroup.ToList().Select(r => r.RoleId))
                .Except(chosenRoleIds);

            rolesToRemove.AddRange(rolesFromGroupToRemove);

            // Set the interaction for the next iteration
            currentInteraction = interactionResult.Result.Interaction;
        }

        foreach (var roleToAdd in rolesToAdd)
        {
            await GrantDiscordRole(ctx, roleToAdd);
        }

        foreach (var roleToRemove in rolesToRemove)
        {
            await RevokeDiscordRole(ctx, roleToRemove);
        }

        // Respond by editing the deferred message with a thank you message
        var responseBuilder = new DiscordWebhookBuilder().WithContent("Enjoy your new roles!");

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

        var user = ctx.Member;

        var userRoles = await _roleService.GetRoleList();

        var roleGroups = userRoles
            .GroupBy(r => r.Group)
            .OrderBy(r => r.Key?.Id ?? int.MaxValue);

        var buttonRow = new List<DiscordComponent>();

        foreach (var roleGroup in roleGroups.ToList())
        {
            string buttonId = roleGroup.Key?.Id.ToString() ?? "none";
            string buttonLabel = roleGroup.Key != null ? $"{roleGroup.Key.Name} roles" : "Ungrouped roles";

            buttonRow.Add(new DiscordButtonComponent(ButtonStyle.Primary, buttonId, buttonLabel));
        }

        var roleGroupButtonsResponse = new DiscordWebhookBuilder()
            .WithContent("Select a role group")
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

        var choiceIsRoleGroup = chosenButtonId != "none";

        var roleGroupToChange = choiceIsRoleGroup
            ? roleGroups.Single(g => g.Key?.Id == uint.Parse(chosenButtonId))
            : roleGroups.Single(g => g.Key == null);

        var isSingleSelect = roleGroupToChange.Key != null && roleGroupToChange.Key.MutuallyExclusive;

        var roleDropdownOptions = new List<DiscordSelectComponentOption>();

        foreach (var userRole in roleGroupToChange.ToList())
        {
            var userHasRole = user.Roles.Select(r => r.Id).Contains(userRole.RoleId);

            roleDropdownOptions.Add(new DiscordSelectComponentOption(userRole.Name, userRole.RoleId.ToString(), userRole.Name, isDefault: userHasRole));
        }

        var maxOptions = isSingleSelect ? 1 : Math.Min(roleDropdownOptions.Count, MaxSelectComponentOptions);
        var minOptions = isSingleSelect ? 1 : 0;

        var roleDropdownId = "dropdown_role";
        var roleDropdownPlaceHolder = isSingleSelect ? "Choose 1 role" : "Choose any roles";
        var roleDropdown = new DiscordSelectComponent(roleDropdownId, roleDropdownPlaceHolder, roleDropdownOptions, minOptions: minOptions, maxOptions: maxOptions);

        var buttonInteractionResultResponse = new DiscordWebhookBuilder()
            .WithContent(roleGroupToChange.Key != null ? $"{roleGroupToChange.Key.Name} roles" : "Ungrouped roles")
            .AddComponents(roleDropdown);

        // Respond by editing the deferred message (a second time)
        theMessage = await theMessageInteractivityResult.Result.Interaction.EditOriginalResponseAsync(buttonInteractionResultResponse);

        var rolesMessageInteractivityResult = await theMessage.WaitForSelectAsync(roleDropdownId);

        // Just acknowledge interaction
        await rolesMessageInteractivityResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var chosenRoleIds = rolesMessageInteractivityResult.Result.Values.Select(ulong.Parse).ToList();

        var rolesToAdd = chosenRoleIds
            .Except(user.Roles.Select(r => r.Id))
            .ToList();

        foreach (var roleToAdd in rolesToAdd)
        {
            await GrantDiscordRole(ctx, roleToAdd);
        }

        var rolesToRemove = user.Roles.Select(r => r.Id)
            .Intersect(roleGroupToChange.ToList().Select(r => r.RoleId))
            .Except(chosenRoleIds);

        foreach (var roleToRemove in rolesToRemove)
        {
            await RevokeDiscordRole(ctx, roleToRemove);
        }

        // Respond by editing the deferred message (a third time)
        var dropdownInteractionResultResponse = new DiscordWebhookBuilder().WithContent("Enjoy your new roles!");

        _ = await rolesMessageInteractivityResult.Result.Interaction.EditOriginalResponseAsync(dropdownInteractionResultResponse);
    }

    private Task GrantDiscordRole(InteractionContext ctx, ulong discordRoleId)
    {
        var member = ctx.Member;
        var guild = ctx.Guild;

        if (!guild.Roles.ContainsKey(discordRoleId))
        {
            throw new ArgumentException($"Role Id {discordRoleId} does not exist");
        }

        var discordRole = ctx.Guild.Roles[discordRoleId];

        return member.GrantRoleAsync(discordRole);
    }

    private Task RevokeDiscordRole(InteractionContext ctx, ulong discordRoleId)
    {
        var member = ctx.Member;
        var guild = ctx.Guild;

        if (!guild.Roles.ContainsKey(discordRoleId))
        {
            throw new ArgumentException($"Role Id {discordRoleId} does not exist");
        }

        var discordRole = ctx.Guild.Roles[discordRoleId];

        return member.RevokeRoleAsync(discordRole);
    }
}
