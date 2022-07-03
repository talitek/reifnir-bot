using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Utils
{
    public static class EmbedBuilderHelper
    {
        public static DiscordEmbed BuildSimpleEmbed(string title, string message)
        {
            var eb = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription(message)
                .WithColor(DiscordConstants.EmbedColor);

            return eb.Build();
        }
    }
}
