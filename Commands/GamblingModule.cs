using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;

namespace Hitbot.Commands;

public class GamblingModule : BaseCommandModule
{
    private EconManager econ { get; }
    private Random rand { get; }

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

    [Command("duel")]
    public async Task DuelCommand(CommandContext ctx, DiscordMember target, int bet = 0)
    {
        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        DiscordMember? caller = ctx.Member;
        DiscordEmoji? triumph = DiscordEmoji.FromName(ctx.Client, ":triumph:");
        string callerstring = Program.GetBalancebookString(caller);
        string targetstring = Program.GetBalancebookString(target);
        if (econ.BookGet(callerstring) < bet || econ.BookGet(targetstring) < bet)
        {
            await ctx.RespondAsync("Insufficient funds on one or both sides.");
            return;
        }

        DiscordMessage? firstmsg =
            await ctx.Channel.SendMessageAsync($"Time for a duel! {target.Nickname}, react with {triumph} to accept!");
        await firstmsg.CreateReactionAsync(triumph);
        var result = await firstmsg.WaitForReactionAsync(target, triumph);

        if (result.TimedOut)
        {
            await ctx.RespondAsync("Timed out.");
            return;
        }

        int[] rnums = new int[3];
        for (int i = 0; i < 3; i++) rnums[i] = rand.Next(1, 10);

        await ctx.Channel.SendMessageAsync("First one to say \"SHOOT\" verbatim after I say \"GO\" wins.");
        await ctx.Channel.SendMessageAsync("Three...");
        await Task.Delay(rnums[0] * 1000);
        await ctx.Channel.SendMessageAsync("Two...");
        await Task.Delay(rnums[1] * 1000);
        await ctx.Channel.SendMessageAsync("One...");
        await Task.Delay(rnums[2] * 1000);
        await ctx.Channel.SendMessageAsync("GO");

        var wa = await interactivity.WaitForMessageAsync(x =>
            x.Channel.Id == ctx.Channel.Id && x.Author.Id == caller.Id || x.Author.Id == target.Id);
        DiscordMessage? winningMessage = wa.Result;

        if (wa.TimedOut || winningMessage is null)
        {
            await ctx.RespondAsync("Nobody won. You slackers.");
            return;
        }

        if (winningMessage.Author.Id == caller.Id)
        {
            DebtGenerousIncr(callerstring, bet);
            econ.BookDecr(targetstring);
        }
        else
        {
            DebtGenerousIncr(targetstring, bet);
            econ.BookDecr(callerstring);
        }

        await ctx.Channel.SendMessageAsync($"{winningMessage.Author.Username} won!");
        await ctx.RespondAsync(
            $"Resulting balances: {econ.BookGet(callerstring)}, {econ.BookGet(targetstring)}");
    }

    private void DebtGenerousIncr(string key, int by = 1)
    {
        econ.BookIncr(key, by);
        if (econ.BookGet(key) < 0 && by != 0)
            econ.BookSet(key, 0);
    }
}