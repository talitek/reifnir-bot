using DSharpPlus.CommandsNext;
using Nellebot.Utils;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public interface IDiscordErrorLogger
    {
        Task LogDiscordError(CommandContext ctx, string errorMessage);
        Task LogDiscordError(EventErrorContext ctx, string errorMessage);
        Task LogDiscordError(string error);
    }
}