using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Nellebot.Services;

namespace Nellebot.CommandModules;
public class RoleSlashModule : ApplicationCommandModule
{
    private const int _maxSelectComponentOptions = 25;
    private readonly RoleService _roleService;

    public RoleSlashModule(RoleService roleService)
    {
        _roleService = roleService;
    }

    [SlashCommand("test", "Test")]
    public async Task TestSlashCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        var aButton = new DiscordButtonComponent(ButtonStyle.Primary, "aButton", "Click me");

        // var interactionBuilder = new DiscordInteractionResponseBuilder()
        //    .WithContent("I does da test")
        //    .AddComponents(aButton);
        var webhookBuilder = new DiscordWebhookBuilder()
            .WithContent("I does da test")
            .AddComponents(aButton);

        // await ctx.CreateResponseAsync(interactionBuilder);
        await ctx.EditResponseAsync(webhookBuilder);

        var message = await ctx.GetOriginalResponseAsync();

        var interactivity = ctx.Client.GetInteractivity();

        var interaction = await interactivity.WaitForButtonAsync(message: message);

        var responseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent("Thanks for clicking me");

        await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
    }

    [SlashCommand("roles", "Chooz da rolez")]
    public async Task Roles(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);

        var userRoles = await _roleService.GetRoleList();

        var roleGroups = userRoles
            .GroupBy(r => r.GroupNumber)
            .OrderBy(r => r.Key.HasValue ? r.Key : int.MaxValue);

        var responseBuilder = new DiscordWebhookBuilder().WithContent("I haz da rolez if yu hav koin");

        foreach (var roleGroup in roleGroups.ToList())
        {
            var options = new List<DiscordSelectComponentOption>();

            foreach (var userRole in roleGroup.ToList())
            {
                options.Add(new DiscordSelectComponentOption(userRole.Name, userRole.Id.ToString(), userRole.Name));
            }

            var maxOptions = roleGroup.Key.HasValue ? 1 : Math.Min(options.Count, _maxSelectComponentOptions);

            string roleDropdownId = $"roles_group_{roleGroup.Key?.ToString() ?? "none"}";

            var roleDropdown = new DiscordSelectComponent(roleDropdownId, $"Chooz role for {roleDropdownId}", options, minOptions: 0, maxOptions: maxOptions);

            responseBuilder = responseBuilder.AddComponents(roleDropdown);
        }

        await ctx.EditResponseAsync(responseBuilder);
    }
}
