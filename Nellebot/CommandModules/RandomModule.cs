using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nellebot.Attributes;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
public class RandomModule : BaseCommandModule
{
    [Command("oi")]
    public Task Oi(CommandContext ctx)
    {
        return ctx.RespondAsync("Oi!");
    }

    [Command("ban")]
    public Task Ban(CommandContext ctx, [RemainingText] string str)
    {
        return ctx.RespondAsync("Why ban them when I can ban you instead?");
    }

    [Command("taco")]
    public Task Taco(CommandContext ctx)
    {
        return ctx.RespondAsync(
            ".\n                :cowboy: are u gonna say howdy\n　＿ノ ヽ ノ＼＿ back or are we\n/      / ⌒ Ｙ ⌒ Ｙ     ヽ gonna have a\n( 　(三ヽ人　 /　　 | fuckin problem\n|　ﾉ⌒＼ ￣￣ヽ　 ノ here partner?\nヽ＿＿＿＞､＿＿_／\n　　 ｜( 王 ﾉ〈\n　　 /ﾐ`ー―彡\n      /      ╰  ╯");
    }

    [Command("meowdy")]
    public Task Meowdy(CommandContext ctx)
    {
        return ctx.RespondAsync(
            ".\n                <:meowcowboy:993855998601220166> are u gonna say meowdy\n　＿ノ ヽ ノ＼＿ back or are we\n/      / ⌒ Ｙ ⌒ Ｙ     ヽ gonna have a\n( 　(三ヽ人　 /　　 | heckin problem\n|　ﾉ⌒＼ ￣￣ヽ　 ノ here purrdner?\nヽ＿＿＿＞､＿＿_／\n　　 ｜( 王 ﾉ〈\n　　 /ﾐ`ー―彡\n      /      ╰  ╯");
    }

    [Command("slap")]
    public Task Slap(CommandContext ctx, DiscordMember member)
    {
        string slapper = ctx.Member?.DisplayName ?? ctx.User.Username;
        string slapee = member.DisplayName;

        return ctx.Channel.SendMessageAsync($"_**{slapper}** slaps **{slapee}** around a bit with a large trout_");
    }
}
