using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;

namespace Hitbot.Commands;

public class GamblingModule : BaseCommandModule
{
    public EconManager econ { get; set; }
    public Random rand { get; set; }

    [Command("slots")]
    public async Task SlotMachine(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        string callerString = econ.GetBalancebookString(caller);
        if (!econ.BalanceBook.ContainsKey(callerString) || econ.BalanceBook[callerString] < 1)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        var possibleemojis = new Dictionary<string, int>
        {
            {":1kbtroll:", 200},
            {":cherries:", 300},
            {":seven:", 400},
            {":fish:", 100},
            {":cowredeyes:", 100},
            {":it:", 50}
        };
        DiscordMessage slotmsg = await ctx.Channel.SendMessageAsync("Time to spin!");
        string currentemoji;
        string[] results = new string[3];
        for (int i = 0; i < 3; i++)
        {
            string choice = possibleemojis.Keys.ToArray()[rand.Next(possibleemojis.Count)];
            results[i] = choice;
            await Task.Delay(i * 1000);
            await slotmsg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, choice));
        }

        foreach (string emoji in possibleemojis.Keys)
        {
            if (results.Count(x => x == emoji) == 2)
            {
                int reward = possibleemojis[emoji] / 2;
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