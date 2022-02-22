using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;

namespace Hitbot.Commands;

public class GamblingModule : BaseCommandModule
{
    public EconManager econ { get; set; }
    public Random rand { get; set; }

    public GamblingModule(EconManager eco, Random rng)
    {
        econ = eco;
        rand = rng;
    }

    [Command("slots")]
    public async Task SlotMachine(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        string callerString = Program.GetBalancebookString(caller);
        if (!econ.BookHasKey(callerString) || econ.BookGet(callerString) < 4)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        econ.BookDecr(callerString, 4);

        var possibleemojis = new Dictionary<string, int>
        {
            {":1kbtroll:", -500},
            {":seven:", 300},
            {":cherries:", 25},
            {":cherries:", 25},
            {":fish:", 20},
            {":fish:", 20},
            {":bigshot:", 20},
            {":bigshot:", 20},
            {":cowredeyes:", 10},
            {":cowredeyes:", 10},
            {":cowredeyes:", 10},
            {":it:", 15},
            {":it:", 15},
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
                econ.BookIncr(callerString, reward);
                await ctx.RespondAsync($"Two {emoji}s! You win {reward} {econ.Currencyname}! Yippee!");
                return;
            }

            if (results.Count(x => x == emoji) == 3)
            {
                int reward = possibleemojis[emoji];
                econ.BookIncr(callerString, reward);
                await ctx.RespondAsync(
                    $"THREE {emoji}s! that's a JACKBOT baybee! {reward} {econ.Currencyname}!!!");
                return;
            }
        }
    }
}