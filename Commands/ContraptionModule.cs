using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;

namespace Hitbot.Commands;

[Group("contraption")]
[RequireGuild]
public class ContraptionModule : BaseCommandModule
{
    private EconManager Econ { get; }
    private ContraptionManager Contraption { get; }
    private Random Rng { get; }
    private LottoManager Lotto { get; }

    public ContraptionModule(EconManager eco, ContraptionManager cont, Random rand, LottoManager lot)
    {
        Econ = eco;
        Contraption = cont;
        Rng = rand;
        Lotto = lot;
    }

    [Command("feed")]
    [Description("Feed the contraption your riches. Who knows what may come...")]
    public async Task FeedCommand(CommandContext ctx, int amount)
    {
        var interactivity = ctx.Client.GetInteractivity();
        string callerstring = Program.GetBalancebookString(ctx.Member);
        if (amount > Econ.BookGet(callerstring))
        {
            await ctx.RespondAsync("Insufficient funds.");
            return;
        }

        Econ.BookDecr(callerstring, amount);
        int result = Contraption.Feed(amount);
        await ctx.RespondAsync("The contraption hums...");
        await Task.Delay(2);
        switch (result)
        {
            case 0:
                await ctx.Channel.SendMessageAsync("..." + Contraption.CurrentValueString());
                return;
            case 1:
                await ctx.Channel.SendMessageAsync("...*And a terrible wailing fills the air.*");
                await Task.Delay(1);
                var econdict = Econ.BookAsDotnetDict();
                foreach (string key in econdict.Keys) Econ.BookSet(key, Econ.BookGet(key) / 4);

                await ctx.Channel.SendMessageAsync("**The Depression is upon us! All balances have been quartered.**");
                break;
            case 2:
                await ctx.Channel.SendMessageAsync("*\"...Change is coming. **YOU.** Say something. Now.\"*");
                var changemessage = await interactivity.WaitForMessageAsync(x =>
                    x.Channel.Equals(ctx.Channel) && x.Author.Equals(ctx.User));
                if (changemessage.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("*\"Failure. You have paid gravely.\"");
                    Econ.BookDecr(callerstring, 750);
                    return;
                }

                string sendthis;
                if (changemessage.Result.Content.Length <= 100)
                    sendthis = changemessage.Result.Content;
                else
                    sendthis = changemessage.Result.Content.Substring(0, 100);

                await ctx.Guild.ModifyAsync(x => x.Name = sendthis);
                await ctx.Channel.SendMessageAsync("Change has come.");
                break;
            case 3:
                await ctx.Channel.SendMessageAsync("*\"...Thank you...\"*");
                Econ.BookIncr(callerstring, 750);
                await Task.Delay(1);
                await ctx.Channel.SendMessageAsync($"...The smell of {Econ.Currencyname} fills the air.");
                break;
            case 4:
                await ctx.Channel.SendMessageAsync("\"*...You.\"*");
                await ctx.Member.GrantRoleAsync(ctx.Guild.GetRole(954621819195904000));
                break;
            case 5:
                await ctx.Channel.SendMessageAsync(
                    "*\"...Delightful. Here is a glimpse of one of my fragments. Enjoy.\"*");
                await Task.Delay(1);
                string[] links =
                {
                    "https://media.discordapp.net/attachments/674390529663959070/954623936329568307/m1cnrBmnMopDcg3ThWbk.png",
                    "https://media.discordapp.net/attachments/674390529663959070/954623978218090496/YPjHguyvENI4VfkwkXXx.png",
                    "https://media.discordapp.net/attachments/674390529663959070/954624242379546624/7XqI12Q0wzDbwT5YHCEk.png",
                    "https://media.discordapp.net/attachments/674390529663959070/954624290244919306/VB1zzkuJO3t8RXA8iieN.png",
                    "https://media.discordapp.net/attachments/674390529663959070/954624699441242132/rxa6ZbWIgge7jc8YHnxZ.png",
                    "https://media.discordapp.net/attachments/674390529663959070/954626230114734100/lagR44DWJ0w4h9PuXjo0.png",
                    "https://media.discordapp.net/attachments/674390529663959070/954626559703146506/mHRPZkuwYflGlS683Dtp--NW1Q8.gif"
                };
                string toSend = links[Rng.Next(links.Length)];
                await ctx.Channel.SendMessageAsync(toSend);
                break;
            case 6:
                await ctx.Channel.SendMessageAsync("*\"...Good luck!\"*");
                Lotto.IncrPot(1000);
                break;
            default:
                await ctx.RespondAsync("John fucked up, and there isn't a command for this situation. Bother him.");
                break;
        }
    }

    [Command("check")]
    public async Task CheckCommand(CommandContext ctx)
    {
        await ctx.RespondAsync(Contraption.CurrentValueString());
    }
}