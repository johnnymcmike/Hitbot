﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;

namespace Hitbot.Commands;

public class EconModule : BaseCommandModule
{
    public EconManager econ { get; set; }

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        econ.WriteBalances();
        return Task.Delay(0);
    }

    [Command("register")]
    [Description("Add your name to the books and get a starting balance")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        DiscordMember? caller = ctx.Member;
        if (econ.BalanceBookHasKey(econ.GetBalancebookString(caller)))
        {
            await ctx.Channel.SendMessageAsync("You are already registered in this server.");
        }
        else
        {
            econ.BalanceBookSet(econ.GetBalancebookString(caller), econ.startingamount);
            await ctx.Channel.SendMessageAsync("Registered :)");
        }
    }

    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx)
    {
        if (econ.BalanceBookHasKey(econ.GetBalancebookString(ctx.Member)))
            await ctx.Channel.SendMessageAsync(
                $"Your balance is {econ.BalanceBookGet(econ.GetBalancebookString(ctx.Member))} {econ.Currencyname}.");
    }

    //overload for getting someone elses balance
    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx,
        [Description("The person whose balance you want to see.")]
        DiscordMember target)
    {
        if (econ.BalanceBookHasKey(econ.GetBalancebookString(target)))
            await ctx.Channel.SendMessageAsync(
                $"{target.Nickname}'s balance is {econ.BalanceBookGet(econ.GetBalancebookString(target))} {econ.Currencyname}.");
    }

    [Command("pay")]
    [Description("Pay currency to another user.")]
    public async Task PayCommand(CommandContext ctx,
        [Description("The @ of the person you intend to pay (you do have to @ them)")]
        DiscordMember recipient,
        [Description("The amount to be paid. No decimals.")]
        int amount)
    {
        amount = Math.Abs(amount);
        DiscordMember? caller = ctx.Member;
        if (caller.Equals(recipient))
        {
            await ctx.Channel.SendMessageAsync("You can't pay yourself!");
            return;
        }

        string callerString = econ.GetBalancebookString(caller);
        string recipientString = econ.GetBalancebookString(recipient);

        if (!econ.BalanceBookHasKey(callerString))
        {
            await ctx.Channel.SendMessageAsync("You aren't registered. Please do so with ~register.");
            return;
        }

        if (recipient.IsBot)
        {
            if (econ.BalanceBookGet(callerString) < amount)
            {
                await ctx.Channel.SendMessageAsync("Insufficient funds.");
                return;
            }

            //TODO: here
            // LottoModule.LottoBook["pot"] += amount;
            econ.BalanceBookDecr(callerString, amount);
            await ctx.Channel.SendMessageAsync(
                $"You tried to pay a bot, so I put your {amount} kromer into the lottery pot. Lol.");
            return;
        }

        if (!econ.BalanceBookHasKey(recipientString))
        {
            await ctx.Channel.SendMessageAsync("Registering recipient...");
            econ.BalanceBookSet(recipientString, econ.startingamount);
        }

        if (econ.BalanceBookGet(callerString) < amount)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        econ.BalanceBookDecr(callerString, amount);
        econ.BalanceBookIncr(recipientString, amount);
        await ctx.Channel.SendMessageAsync(
            $"Paid {amount} {econ.Currencyname} to {recipient.Nickname} (you now have {econ.BalanceBookGet(callerString)}, " +
            $"they have {econ.BalanceBookGet(recipientString)})");
    }

    [Command("print")]
    [Description("Owner only. Prints new currency and gives to specified user.")]
    [RequireGuild]
    [RequireOwner]
    public async Task PrintCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        econ.BalanceBookIncr(econ.GetBalancebookString(recipient), amount);
        await ctx.Channel.SendMessageAsync($"{amount} new currency given to {recipient.Nickname}");
    }

    [Command("leaderboard")]
    [Description("Display a list of registered users sorted by descending balance.")]
    public async Task LeaderboardCommand(CommandContext ctx)
    {
        var wa = econ.DotnetDictFromRedisHash("balances");
        foreach (var VARIABLE in wa)
        {
            Console.WriteLine($"{VARIABLE.Key}, {VARIABLE.Value}");
        }
        // string result = "";
        // InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        // var pages = interactivity.GeneratePagesInEmbed(result);
        //
        // await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }
}