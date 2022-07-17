using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Utils
{
    public class DiscordConstants
    {
        public static readonly DateTimeOffset DiscordEpoch = new(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public static long DiscordEpochMs = DiscordEpoch.ToUnixTimeMilliseconds();

        public const int MaxMessageLength = 2000;
        public const int MaxEmbedContentLength = 4096;
        public const int DefaultEmbedColor = 2346204; // #23ccdc
        public const int ErrorEmbedColor = 14431557; // #dc3545
        public const int WarningEmbedColor = 16612884; // #fd7e14
    }
}
