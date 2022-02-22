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
        if (!econ.BookHasKey(callerString) || econ.BookGet(callerString) < 1)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        econ.BookDecr(callerString);

        var emojidefs = new List<KeyValuePair<string, int>>
        {
            new(":1kbtroll:", -420),
            new(":seven:", 300),
            new(":cherries:", 15),
            new(":cherries:", 15),
            new(":fish:", 20),
            new(":fish:", 20),
            new(":bigshot:", 15),
            new(":bigshot:", 15),
            new(":cowredeyes:", 10),
            new(":cowredeyes:", 10),
            new(":cowredeyes:", 10),
            new(":it:", 5),
            new(":it:", 5),
            new(":it:", 5)
        };
        string slotresultstr = " ";
        DiscordMessage slotmsg = await ctx.Channel.SendMessageAsync("Spinning...");
        await Task.Delay(2000);

        string[] possemo = emojidefs.Select(variable => variable.Key).ToArray();

        string[] results = new string[3];
        for (int i = 0; i < 3; i++)
        {
            string choice = possemo[rand.Next(emojidefs.Count)];
            results[i] = choice;
            await Task.Delay(i * 1000);
            slotresultstr += DiscordEmoji.FromName(ctx.Client, choice).ToString();
            await slotmsg.ModifyAsync(slotresultstr);
        }

        for (int i = 0; i < possemo.Length; i++)
        {
            string emoji = possemo[i];
            if (results.Count(x => x == emoji) == 2)
            {
                int reward = emojidefs[i].Value / 3;
                econ.BookIncr(callerString, reward);
                await ctx.RespondAsync($"Two {emoji}s! You win {reward} {econ.Currencyname}! Yippee!");
                return;
            }

            if (results.Count(x => x == emoji) == 3)
            {
                int reward = emojidefs[i].Value;
                econ.BookIncr(callerString, reward);
                await ctx.RespondAsync(
                    $"THREE {emoji}s! that's a JACKBOT baybee! {reward} {econ.Currencyname}!!!");
                return;
            }
        }
    }
}