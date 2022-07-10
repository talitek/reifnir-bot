using DSharpPlus.CommandsNext;
using Nellebot.Utils;
using System.Threading.Tasks;

namespace Nellebot.Services.Loggers
{
    public interface IDiscordErrorLogger
    {
        Task LogCommandError(CommandContext ctx, string errorMessage);
        Task LogEventError(EventContext ctx, string errorMessage);
        Task LogError(string error, string errorMessage);
        Task LogError(string errorMessage);
        Task LogWarning(string warning, string warningMessage);
    }
}