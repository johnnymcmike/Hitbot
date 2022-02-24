using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;

namespace Hitbot.Commands;

[Group("lotto")]
[Description("Lottery commands.")]
[RequireGuild]
public class LottoModule : BaseCommandModule
{
    private readonly int drawp;

    private readonly int ticketp;
    private LottoManager Lotto { get; }

    private EconManager Econ { get; }

    private Random Rand { get; }
    private DailyFlagManager Dailies { get; }

    public LottoModule(LottoManager lotto, EconManager eco, Random rng, DailyFlagManager dail)
    {
        Lotto = lotto;
        Econ = eco;
        Rand = rng;
        Dailies = dail;
        ticketp = Lotto.LottoTicketprice;
        drawp = Lotto.LottoDrawprice;
    }


    [Command("buyticket")]
    [Description("Buy a lottery ticket for a preset fee.")]
    public async Task EnterLottoCommand(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        string callerstring = Program.GetBalancebookString(caller);
        int totalcost = Math.Abs(ticketp);
        if (Lotto.BookGet(callerstring) > 1) return;
        if (Econ.BookGet(callerstring) < totalcost)
        {
            await ctx.RespondAsync("Insufficient funds.");
        }
        else
        {
            Econ.BookDecr(callerstring, totalcost);
            Lotto.BookIncr(callerstring);
            Lotto.BookIncr("pot", totalcost);
            await ctx.RespondAsync("You have signed up for the lottery. " +
                                   $"Your new balance is {Econ.BookGet(callerstring)}");
        }
    }

    [Command("draw")]
    [Description(
        "For a fee, draw the lottery. This either pays out the whole pot to one lucky winner, or nobody gets it and the pot is preserved.")]
    public async Task DrawLottoCommand(CommandContext ctx)
    {
        if (Econ.BookGet(Program.GetBalancebookString(ctx.Member)) < ticketp)
        {
            await ctx.RespondAsync(
                $"Insufficient funds to start a draw. Drawing costs {drawp} {Econ.Currencyname}.");
            return;
        }

        if (Dailies.DailyExists("any", "lottodraw"))
        {
            await ctx.RespondAsync("Lotto was already drawn today.");
            return;
        }

        int reward = Lotto.BookGet("pot") + drawp;
        Econ.BookDecr(Program.GetBalancebookString(ctx.Member), drawp);
        var lottoDict = Lotto.BookAsDotnetDict();

        Dictionary<string, double> chances = new();
        lottoDict.Remove("pot");
        int totaltickets = lottoDict.Sum(entry => entry.Value);
        try
        {
            foreach (var entry in lottoDict) chances.Add(entry.Key, (double) entry.Value / totaltickets);
        }
        catch (DivideByZeroException)
        {
            await ctx.RespondAsync("Nobody is entered.");
            return;
        }

        double rnum;
        Console.WriteLine("Drawing lotto...");
        chances = chances.OrderBy(_ => Rand.Next()).ToDictionary(item => item.Key, item => item.Value);
        foreach (var entry in chances)
        {
            rnum = Rand.NextDouble();
            if (entry.Value == 1)
            {
                await ctx.RespondAsync("Need more people to draw.");
                return;
            }

            Console.WriteLine($"{entry.Key.Split("/")[1]}'s chances value is {entry.Value} and random is {rnum}");
            if (entry.Value > rnum)
            {
                Econ.BookIncr(entry.Key, reward);
                Lotto.BookClear();
                await ctx.Channel.SendMessageAsync(
                    $"{entry.Key.Split("/")[1]} has won the lottery, " +
                    $"earning {reward} {Econ.Currencyname}! Congrats!");
                Dailies.SetDaily("any", "lottodraw");
                return;
            }
        }


        //if nobody got picked above
        Lotto.BookClear();
        Lotto.BookSet("pot", reward);
        await ctx.Channel.SendMessageAsync($"Nobody won. The pot has remained at {reward}. Better luck next time!");
        Dailies.SetDaily("any", "lottodraw");
    }

    [Command("view")]
    [Description("Display all entered users and see the pot.")]
    public async Task ViewLottoCommand(CommandContext ctx)
    {
        var lottoDict = Lotto.BookAsDotnetDict();
        var sorted = from entry in lottoDict orderby entry.Value descending select entry;
        string result = "";
        result += $"The pot is {lottoDict["pot"]} {Econ.Currencyname}.\n \n";
        foreach (var entry in sorted)
        {
            if (entry.Key.Equals("pot")) continue;
            result += $"{entry.Key.Split("/")[1]} has {entry.Value} tickets\n";
        }

        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        var pages = interactivity.GeneratePagesInEmbed(result);

        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }
}