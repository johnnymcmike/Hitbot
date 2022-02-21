using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;

namespace Hitbot.Commands;

public class GamblingModule : BaseCommandModule
{
    public EconManager econ { get; set; }
    public Random rand { get; set; }


    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        econ.WriteBalances();
        return Task.Delay(0);
    }

    [Command("slots")]
    public async Task SlotMachine(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        string callerString = econ.GetBalancebookString(caller);
        if (!econ.BalanceBook.ContainsKey(callerString) || econ.BalanceBook[callerString] < 2)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        econ.BalanceBook[callerString] -= 2;

        var possibleemojis = new Dictionary<string, int>
        {
            {":1kbtroll:", -30},
            {":cherries:", 30},
            {":seven:", 50},
            {":fish:", 40},
            {":cowredeyes:", 20},
            {":it:", 15}
        };
        string slotresultstr = " ";
        DiscordMessage slotmsg = await ctx.Channel.SendMessageAsync("Spinning...");
        await Task.Delay(2000);

        string[] results = new string[3];
        for (int i = 0; i < 3; i++)
        {
            string choice = possibleemojis.Keys.ToArray()[rand.Next(possibleemojis.Count)];
            results[i] = choice;
            await Task.Delay(i * 1000);
            slotresultstr += DiscordEmoji.FromName(ctx.Client, choice).ToString();
            await slotmsg.ModifyAsync(slotresultstr);
        }

        foreach (string emoji in possibleemojis.Keys)
        {
            if (results.Count(x => x == emoji) == 2)
            {
                int reward = possibleemojis[emoji] / 3;
                econ.BalanceBook[callerString] += reward;
                await ctx.Channel.SendMessageAsync($"Two {emoji}s! You win {reward} {econ.Currencyname}! Yippee!");
                return;
            }

            if (results.Count(x => x == emoji) == 3)
            {
                int reward = possibleemojis[emoji];
                econ.BalanceBook[callerString] += reward;
                await ctx.Channel.SendMessageAsync(
                    $"THREE {emoji}s! that's a JACKBOT baybee! {reward} {econ.Currencyname}!!!");
                return;
            }
        }
    }
}