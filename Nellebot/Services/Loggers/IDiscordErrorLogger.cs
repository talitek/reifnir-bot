using DSharpPlus.CommandsNext;
using Nellebot.Utils;
using System.Threading.Tasks;

namespace Nellebot.Services.Loggers
{
    public interface IDiscordErrorLogger
    {
        Task LogCommandError(CommandContext ctx, string errorMessage);
        Task LogEventError(EventErrorContext ctx, string errorMessage);
        Task LogError(string error);
    }
}