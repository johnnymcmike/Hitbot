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
    public GamesManager Game { get; set; }

    public EconManager econ { get; set; }

    public LottoModule(GamesManager game, EconManager eco)
    {
        Game = game;
        econ = eco;
        ticketp = Game.lottoTicketprice;
        drawp = Game.lottoDrawprice;
    }

    public Random Rand { get; set; }

    [Command("buyticket")]
    [Description("Buy a lottery ticket for a preset fee. You are able to buy multiple.")]
    public async Task EnterLottoCommand(CommandContext ctx,
        [Description("Amount of tickets to buy. Defaults to 1.")]
        int numtickets = 1)
    {
        DiscordMember? caller = ctx.Member;
        string callerstring = Program.GetBalancebookString(caller);
        int totalcost = Math.Abs(numtickets * ticketp);
        if (econ.BookGet(callerstring) < totalcost)
        {
            await ctx.RespondAsync("Insufficient funds.");
        }
        else
        {
            econ.BookDecr(callerstring, totalcost);
            Game.BookIncr(callerstring, numtickets);
            Game.BookIncr("pot", totalcost);
            await ctx.RespondAsync($"You have signed up for the lottery with {numtickets} tickets. " +
                                   $"Your new balance is {econ.BookGet(callerstring)}");
        }
    }

    [Command("draw")]
    [Description(
        "For a fee, draw the lottery. This either pays out the whole pot to one lucky winner, or nobody gets it and the pot is preserved.")]
    public async Task DrawLottoCommand(CommandContext ctx)
    {
        if (econ.BookGet(Program.GetBalancebookString(ctx.Member)) < ticketp)
        {
            await ctx.RespondAsync(
                $"Insufficient funds to start a draw. Drawing costs {drawp} {econ.Currencyname}.");
            return;
        }

        int reward = Game.BookGet("pot") + drawp;
        econ.BookDecr(Program.GetBalancebookString(ctx.Member), drawp);

        Dictionary<string, double> chances = new();
        var lottoDict = Game.BookAsDotnetDict();
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
        chances = chances.OrderBy(x => Rand.Next()).ToDictionary(item => item.Key, item => item.Value);
        foreach (var entry in chances)
        {
            rnum = Rand.NextDouble();
            Console.WriteLine($"{entry.Key.Split("/")[1]}'s chances value is {entry.Value} and random is {rnum}");
            if (entry.Value > rnum)
            {
                econ.BookIncr(entry.Key, reward);
                Game.BookClear();
                await ctx.Channel.SendMessageAsync(
                    $"{entry.Key.Split("/")[1]} has won the lottery, " +
                    $"earning {reward} {econ.Currencyname}! Congrats!");
                return;
            }
        }


        //if nobody got picked above
        Game.BookClear();
        Game.BookSet("pot", reward);
        await ctx.Channel.SendMessageAsync($"Nobody won. The pot has remained at {reward}. Better luck next time!");
    }

    [Command("view")]
    [Description("Display all entered users and see the pot.")]
    public async Task ViewLottoCommand(CommandContext ctx)
    {
        var lottoDict = Game.BookAsDotnetDict();
        var sorted = from entry in lottoDict orderby entry.Value descending select entry;
        string result = "";
        result += $"The pot is {lottoDict["pot"]} {econ.Currencyname}.\n \n";
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