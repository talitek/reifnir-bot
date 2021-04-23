using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Options;
using Nellebot;
using Nellebot.Attributes;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    public class HelpModule : BaseCommandModule
    {
        private readonly BotOptions _options;

        public HelpModule(
            IOptions<BotOptions> options)
        {
            _options = options.Value;
        }

        [Command("help")]
        [Description("Return some help")]
        public Task GetHelp(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine("**Roles on NELLE**");
            sb.AppendLine($"On NELLE, you can assign roles to yourself using the `{commandPrefix}role` command followed by the `name` of the role.");
            sb.AppendLine($"To unassign a role from yourself, type the same command again.");

            sb.AppendLine();
            sb.AppendLine("Examples:");
            sb.AppendLine($"`{commandPrefix}role beginner`");
            sb.AppendLine($"`{commandPrefix}role norsk`");
            sb.AppendLine($"`{commandPrefix}role utlending`");

            sb.AppendLine();
            sb.AppendLine("If you have any problems, you can always contact one of the moderators.");

            sb.AppendLine();
            sb.AppendLine("Reifnir source code: github.com/NELLE-reifnir-bot/reifnir-bot");

            var result = sb.ToString();

            return ctx.RespondAsync(result);
        }
    }
}
