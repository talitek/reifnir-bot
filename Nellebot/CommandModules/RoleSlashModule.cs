using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Nellebot.CommandModules;
public class RoleSlashModule : ApplicationCommandModule
{
    private const int _maxSelectComponentOptions = 25;

    [SlashCommand("test", "Test")]
    public async Task TestSlashCommand(InteractionContext ctx)
    {
        var aButton = new DiscordButtonComponent(ButtonStyle.Primary, "aButton", "Click me");

        var interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithContent("I does da test")
            .AddComponents(aButton);

        await ctx.CreateResponseAsync(interactionBuilder);
    }

    [SlashCommand("roles", "Chooz da rolez")]
    public async Task Roles(InteractionContext ctx)
    {
        // await ctx.DeferAsync(true);
        var rolesDropdown = new DiscordRoleSelectComponent("roles", "Chooz da rolez", minOptions: 0, maxOptions: _maxSelectComponentOptions);

        var interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithContent("I haz da rolez if yu hav koin")
            .AddComponents(rolesDropdown);

        await ctx.CreateResponseAsync(interactionBuilder);
    }
}
