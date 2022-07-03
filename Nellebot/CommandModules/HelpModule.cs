using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Options;
using Nellebot;
using Nellebot.Attributes;
using Nellebot.Utils;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    [Group("help")]
    public class HelpModule : BaseCommandModule
    {
        private readonly BotOptions _options;

        public HelpModule(
            IOptions<BotOptions> options)
        {
            _options = options.Value;
        }

        [GroupCommand]
        public Task Help(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine($"On NELLE, you can assign roles to yourself using the `{commandPrefix}role` command followed by the `name` of the role.");
            sb.AppendLine($"For example: `{commandPrefix}role beginner`");
            sb.AppendLine($"To unassign a role from yourself, type the same command again.");
            sb.AppendLine($"To view all the available roles, type `{commandPrefix}roles`.");

            sb.AppendLine();
            sb.AppendLine("If you have any problems, you can always contact one of the moderators.");

            sb.AppendLine();
            sb.AppendLine("Other useful commands:");
            sb.AppendLine($"`{commandPrefix}bm [query] .. Search the bokmål dictionary`");
            sb.AppendLine($"`{commandPrefix}nn [query] .. Search the nynorsk dictionary`");

            sb.AppendLine();
            sb.AppendLine("For staff only:");
            sb.AppendLine($"`{commandPrefix}help user-role`");
            sb.AppendLine($"`{commandPrefix}help admin-misc`");

            sb.AppendLine();
            sb.AppendLine("Reifnir source code: [github.com/NELLE-reifnir-bot/reifnir-bot](https://github.com/NELLE-reifnir-bot/reifnir-bot)");

            var eb = EmbedBuilderHelper.BuildSimpleEmbed("Roles on NELLE", sb.ToString());

            return ctx.RespondAsync(eb);
        }

        [Command("user-role")]
        public Task HelpUserRole(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var command = $"{_options.CommandPrefix}user-role";

            sb.AppendLine($"`{command} create-role [role] [?alias-list]`");
            sb.AppendLine($"`{command} delete-role [role]`");
            sb.AppendLine($"`{command} get-role [role]`");
            sb.AppendLine($"`{command} add-alias [role] [alias-name]`");
            sb.AppendLine($"`{command} remove-alias [role] [alias-name]`");
            sb.AppendLine($"`{command} set-group [role] [group-number]`");
            sb.AppendLine($"`{command} unset-group [role]`");
            sb.AppendLine($"`{command} update-roles`");

            sb.AppendLine();
            sb.AppendLine($"Command arguments:");
            sb.AppendLine($"`role           .. Discord role name, Discord role Id or Discord role @mention`");
            sb.AppendLine($"`alias-name     .. User role alias (used when assigning role)`");
            sb.AppendLine($"`alias-list     .. Alias names (comma separated values, optional)`");
            sb.AppendLine($"`group-number   .. User role group number (positive whole number)`");

            var eb = EmbedBuilderHelper.BuildSimpleEmbed("User role commands", sb.ToString());

            return ctx.RespondAsync(eb);
        }

        [Command("admin-misc")]
        public Task HelpAdminMisc(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var command = $"{_options.CommandPrefix}admin";

            sb.AppendLine($"`{command} nickname [name]`");
            sb.AppendLine($"`   Change Reifnir's nickname`");
            sb.AppendLine();

            sb.AppendLine($"`{command} set-greeting-message [message]`");
            sb.AppendLine($"`   Set the greeting message that Reifnir welcomes new users with.`");
            sb.AppendLine($"`   Use the token $USER to @mention the new user in the message`");
            sb.AppendLine();

            sb.AppendLine($"`{command} list-award-channels`");
            sb.AppendLine($"`   List the channels where Reifnir keeps track of cookies`");
            sb.AppendLine();

            var eb = EmbedBuilderHelper.BuildSimpleEmbed("User role commands", sb.ToString());

            return ctx.RespondAsync(eb);
        }
    }
}
