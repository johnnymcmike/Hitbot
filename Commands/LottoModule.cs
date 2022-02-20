using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hitbot.Services;
using Newtonsoft.Json;

namespace Hitbot.Commands;

[Group("lotto")]
[Description("Lottery commands.")]
[RequireGuild]
public class LottoModule : BaseCommandModule
{
    private Dictionary<string, int> LottoBook;
    public Random rand { get; set; }
    private readonly int lottoTicketprice;
    private readonly int lottoDrawprice;

    public LottoModule()
    {
        if (File.Exists("lotto.json"))
            LottoBook = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText("lotto.json"))!;
        else
            ClearLottoBook();

        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            lottoTicketprice = int.Parse(config["lottoticketprice"]);
            lottoDrawprice = int.Parse(config["lottodrawprice"]);
        }
        else
        {
            throw new Exception("no config.json found");
        }
    }

    public EconManager econ { get; set; }

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        econ.WriteBalances();
        WriteLottoBook();
        return Task.Delay(0);
    }

    private void WriteLottoBook()
    {
        File.WriteAllText("lotto.json", JsonConvert.SerializeObject(LottoBook));
    }

    private void ClearLottoBook()
    {
        LottoBook = new Dictionary<string, int>();
        LottoBook.Add("pot", 0);
        WriteLottoBook();
    }

    [Command("buyticket")]
    [Description("Buy a lottery ticket for a preset fee. You are able to buy multiple.")]
    public async Task EnterLottoCommand(CommandContext ctx,
        [Description("Amount of tickets to buy. Defaults to 1.")]
        int amount = 1)
    {
        DiscordMember? caller = ctx.Member;
        string callerstring = econ.GetBalancebookString(caller);
        int totalcost = Math.Abs(amount * lottoTicketprice);
        if (econ.BalanceBook[callerstring] < totalcost)
        {
            await ctx.RespondAsync("Insufficient funds.");
        }
        else
        {
            if (!LottoBook.ContainsKey(econ.GetBalancebookString(caller)))
            {
                econ.BalanceBook[callerstring] -= totalcost;
                LottoBook.Add(callerstring, amount);
                LottoBook["pot"] += totalcost;
                await ctx.RespondAsync($"You have signed up for the lottery with {amount} tickets. " +
                                       $"Your new balance is {econ.BalanceBook[callerstring]}");
            }
            else
            {
                econ.BalanceBook[callerstring] -= totalcost;
                LottoBook[callerstring] += amount;
                LottoBook["pot"] += totalcost;
                await ctx.RespondAsync(
                    $"You bought {amount} more lottery tickets, leaving you with {LottoBook[callerstring]}. " +
                    $"Your new balance is {econ.BalanceBook[callerstring]}");
            }
        }
    }

    [Command("draw")]
    [Description(
        "For a fee, draw the lottery. This either pays out the whole pot to one lucky winner, or nobody gets it and the pot is preserved.")]
    public async Task DrawLottoCommand(CommandContext ctx)
    {
        if (econ.BalanceBook[econ.GetBalancebookString(ctx.Member)] < lottoTicketprice)
        {
            await ctx.RespondAsync(
                $"Insufficient funds to start a draw. Drawing costs {lottoDrawprice} {econ.Currencyname}.");
            return;
        }

        int reward = LottoBook["pot"] + lottoDrawprice;
        Dictionary<string, double> chances = new();
        LottoBook.Remove("pot");
        int totaltickets = LottoBook.Sum(entry => entry.Value);
        try
        {
            foreach (var entry in LottoBook) chances.Add(entry.Key, (double) entry.Value / totaltickets);
        }
        catch (DivideByZeroException)
        {
            await ctx.RespondAsync("Nobody is entered.");
            return;
        }

        double rnum;
        Console.WriteLine("Drawing lotto...");
        chances = chances.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value);
        foreach (var entry in chances)
        {
            rnum = rand.NextDouble();
            Console.WriteLine($"{entry.Key.Split("/")[1]}'s chances value is {entry.Value} and random is {rnum}");
            if (entry.Value > rnum)
            {
                econ.BalanceBook[entry.Key] += reward;
                ClearLottoBook();
                await ctx.Channel.SendMessageAsync(
                    $"{entry.Key.Split("/")[1]} has won the lottery, " +
                    $"earning {reward} {econ.Currencyname}! Congrats!");
                return;
            }
        }


        //if nobody got picked above
        ClearLottoBook();
        LottoBook["pot"] = reward;
        await ctx.Channel.SendMessageAsync($"Nobody won. The pot has remained at {reward}. Better luck next time!");
    }

    [Command("view")]
    [Description("Display all entered users and see the pot.")]
    public async Task ViewLottoCommand(CommandContext ctx)
    {
        var sorted = from entry in LottoBook orderby entry.Value descending select entry;
        string result = "";
        result += $"The pot is {LottoBook["pot"]} {econ.Currencyname}.\n \n";
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