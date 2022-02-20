﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;
using Newtonsoft.Json;

namespace Hitbot.Commands;

[Group("lotto")]
[Description("Lottery commands.")]
public class LottoModule : BaseCommandModule
{
    private Dictionary<string, int> LottoBook;
    private readonly int ticketprice;

    public LottoModule()
    {
        if (File.Exists("lotto.json"))
            LottoBook = JsonConvert.DeserializeObject<Dictionary<string, int>>("lotto.json")!;
        else
            ClearLottoBook();

        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            ticketprice = int.Parse(config["ticketprice"]);
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
    public async Task EnterLottoCommand(CommandContext ctx, int amount = 1)
    {
        DiscordMember? caller = ctx.Member;
        string callerstring = econ.GetBalancebookString(caller);
        int totalcost = Math.Abs(amount * ticketprice);
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
}