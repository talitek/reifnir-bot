﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models.UserRoles;
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

    [SlashCommand("role", "Choose a new role or remove an existing role")]
    public async Task SetSelfRole(InteractionContext ctx, [Option("role", "Role name or alias")] string role)
    {
        var member = ctx.Member ?? throw new Exception($"{nameof(ctx.Member)} is null");

        await ctx.DeferAsync(true);

        var userRole = await _roleService.GetUserRoleByNameOrAlias(role);

        if (userRole == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{role}** role does not exist"));
            return;
        }

        var existingDiscordRole = member.Roles.SingleOrDefault(r => r.Id == userRole.RoleId);

        if (existingDiscordRole == null)
        {
            await AddSelfDiscordRole(ctx, userRole);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Enjoy your new role!"));
        }
        else
        {
            await member.RevokeRoleAsync(existingDiscordRole);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are now one role lighter."));
        }
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

    [SlashCommand("roles", "Choose one or more roles")]
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
            .OrderBy(r => r.Key?.Id ?? int.MaxValue)
            .ToList();

        var rolesToAdd = new List<ulong>();
        var rolesToRemove = new List<ulong>();

        foreach (var roleGroup in roleGroups)
        {
            var isSingleSelect = roleGroup.Key != null && roleGroup.Key.MutuallyExclusive;

            // TODO: Temporary(tm) hack. Mandatory should be a flag on the group
            var isMandatory = roleGroup == roleGroups.First();

            var roleDropdownOptions = new List<DiscordSelectComponentOption>();

            foreach (var userRole in roleGroup.ToList())
            {
                var userHasRole = user.Roles.Select(r => r.Id).Contains(userRole.RoleId);

                roleDropdownOptions.Add(new DiscordSelectComponentOption(userRole.Name, userRole.RoleId.ToString(), userRole.Name, isDefault: userHasRole));
            }

            var maxOptions = isSingleSelect ? 1 : Math.Min(roleDropdownOptions.Count, MaxSelectComponentOptions);
            var minOptions = isSingleSelect ? 1 : 0;

            const string roleDropdownId = "dropdown_role";
            var roleDropdownPlaceHolder = isSingleSelect ? $"Choose 1 role" : "Choose any roles";
            var roleDropdown = new DiscordSelectComponent(roleDropdownId, roleDropdownPlaceHolder, roleDropdownOptions, minOptions: minOptions, maxOptions: maxOptions);

            const string skipButtonId = "skip_button";
            var skipButton = new DiscordButtonComponent(ButtonStyle.Secondary, skipButtonId, "Skip", disabled: isMandatory);

            var interactionResultResponse = new DiscordWebhookBuilder()
                .WithContent(roleGroup.Key != null ? $"{roleGroup.Key.Name} roles" : "Ungrouped roles")
                .AddComponents(roleDropdown)
                .AddComponents(skipButton);

            // Respond by editing the deferred message
            var theMessage = await ctx.Interaction.EditOriginalResponseAsync(interactionResultResponse);

            // Wait for a choice, either dropdown selection or skip button
            var messageCancellationTokenSource = new CancellationTokenSource();
            var waitForSelectTask = theMessage.WaitForSelectAsync(roleDropdownId, messageCancellationTokenSource.Token);
            var waitForButtonTask = theMessage.WaitForButtonAsync(skipButtonId, messageCancellationTokenSource.Token);

            var interactionResult = (await Task.WhenAny(waitForSelectTask, waitForButtonTask)).Result;
            messageCancellationTokenSource.Cancel();

            // Acknowledge the interaction.
            // This deferred message will be handled either in the next interation or outside the loop when we're done
            await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            var isSkipped = interactionResult.Result.Id == skipButtonId;

            if (isSkipped) continue;

            var chosenRoleIds = interactionResult.Result.Values.Select(ulong.Parse).ToList();

            var rolesFromGroupToAdd = chosenRoleIds
                .Except(user.Roles.Select(r => r.Id))
                .ToList();

            rolesToAdd.AddRange(rolesFromGroupToAdd);

            var rolesFromGroupToRemove = user.Roles.Select(r => r.Id)
                .Intersect(roleGroup.ToList().Select(r => r.RoleId))
                .Except(chosenRoleIds);

            rolesToRemove.AddRange(rolesFromGroupToRemove);
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

        await ctx.Interaction.EditOriginalResponseAsync(responseBuilder);
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

    private async Task AddSelfDiscordRole(InteractionContext ctx, UserRole userRole)
    {
        var member = ctx.Member ?? throw new Exception($"{nameof(ctx.Member)} is null");
        var guild = ctx.Guild;

        // Check that the role exists in the guild
        if (!guild.Roles.ContainsKey(userRole.RoleId))
        {
            throw new ArgumentException($"Role Id {userRole.RoleId} does not exist");
        }

        // Assign new role
        var discordRole = ctx.Guild.Roles[userRole.RoleId];

        await member.GrantRoleAsync(discordRole);

        // If user role is part of a mutually exclusive group, remove all other roles from the group
        await RemoveAllOtherRolesInMutexGroup(userRole, member, guild);
    }

    private async Task RemoveAllOtherRolesInMutexGroup(UserRole userRole, DiscordMember member, DiscordGuild guild)
    {
        if (userRole.Group == null || userRole.Group.MutuallyExclusive == false)
            return;

        var otherRolesInGroup = (await _roleService.GetUserRolesByGroup(userRole.Group.Id))
                                            .Where(r => r.Id != userRole.Id);

        var discordRolesInGroupToRemove = member.Roles
                            .Where(r => otherRolesInGroup.Select(r => r.RoleId)
                                        .ToList()
                                        .Contains(r.Id))
                            .ToList();

        foreach (var discordRoleToRemove in discordRolesInGroupToRemove)
        {
            await member.RevokeRoleAsync(discordRoleToRemove);
        }
    }
}
