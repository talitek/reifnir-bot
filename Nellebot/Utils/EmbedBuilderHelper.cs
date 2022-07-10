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
            return BuildSimpleEmbed(title, message, DiscordConstants.DefaultEmbedColor);
        }

        public static DiscordEmbed BuildSimpleEmbed(string title, string message, int color)
        {
            var eb = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription(message)
                .WithColor(color);

            return eb.Build();
        }
    }
}
