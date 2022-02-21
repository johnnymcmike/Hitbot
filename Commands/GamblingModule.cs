using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;

namespace Hitbot.Commands;

public class GamblingModule : BaseCommandModule
{
    public EconManager econ { get; set; }

    [Command("slots")]
    public async Task SlotMachine(CommandContext ctx, int bet = 1)
    {
        DiscordMember? caller = ctx.Member;
        string callerString = econ.GetBalancebookString(caller);
        if (!econ.BalanceBook.ContainsKey(callerString) || econ.BalanceBook[callerString] < bet)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        DiscordMessage? slotmsg = await ctx.Channel.SendMessageAsync("\\/get trolled");
        await slotmsg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":1kbtroll:"));
    }
}