using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
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
        if (econ.BalanceBook.ContainsKey(econ.GetBalancebookString(caller)))
        {
            await ctx.Channel.SendMessageAsync("You are already registered in this server.");
        }
        else
        {
            econ.BalanceBook.Add(econ.GetBalancebookString(caller), econ.startingamount);
            await ctx.Channel.SendMessageAsync("Registered :)");
        }
    }

    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx)
    {
        if (econ.BalanceBook.ContainsKey(econ.GetBalancebookString(ctx.Member)))
            await ctx.Channel.SendMessageAsync(
                $"Your balance is {econ.BalanceBook[econ.GetBalancebookString(ctx.Member)]} {econ.Currencyname}.");
    }

    //overload for getting someone elses balance
    [Command("balance")]
    [Description("Gets your current balance.")]
    public async Task BalanceCommand(CommandContext ctx,
        [Description("The person whose balance you want to see.")]
        DiscordMember target)
    {
        if (econ.BalanceBook.ContainsKey(econ.GetBalancebookString(target)))
            await ctx.Channel.SendMessageAsync(
                $"{target.Nickname}'s balance is {econ.BalanceBook[econ.GetBalancebookString(target)]} {econ.Currencyname}.");
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

        if (!econ.BalanceBook.ContainsKey(callerString))
        {
            await ctx.Channel.SendMessageAsync("You aren't registered. Please do so with ~register.");
            return;
        }

        if (recipient.IsBot)
        {
            if (econ.BalanceBook[callerString] < amount)
            {
                await ctx.Channel.SendMessageAsync("Insufficient funds.");
                return;
            }

            LottoModule.LottoBook["pot"] += amount;
            econ.BalanceBook[callerString] -= amount;
            await ctx.Channel.SendMessageAsync(
                $"You tried to pay a bot, so I put your {amount} kromer into the lottery pot. Lol.");
            return;
        }

        if (!econ.BalanceBook.ContainsKey(recipientString))
        {
            await ctx.Channel.SendMessageAsync("Registering recipient...");
            econ.BalanceBook.Add(recipientString, econ.startingamount);
        }

        if (econ.BalanceBook[callerString] < amount)
        {
            await ctx.Channel.SendMessageAsync("Insufficient funds.");
            return;
        }

        econ.BalanceBook[callerString] -= amount;
        econ.BalanceBook[recipientString] += amount;
        await ctx.Channel.SendMessageAsync(
            $"Paid {amount} {econ.Currencyname} to {recipient.Nickname} (you now have {econ.BalanceBook[callerString]}, " +
            $"they have {econ.BalanceBook[recipientString]})");
    }

    [Command("print")]
    [Description("Owner only. Prints new currency and gives to specified user.")]
    [RequireGuild]
    [RequireOwner]
    public async Task PrintCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        econ.BalanceBook[econ.GetBalancebookString(recipient)] += amount;
        await ctx.Channel.SendMessageAsync($"{amount} new currency given to {recipient.Nickname}");
    }

    [Command("leaderboard")]
    [Description("Display a list of registered users sorted by descending balance.")]
    public async Task LeaderboardCommand(CommandContext ctx)
    {
        var sorted = from entry in econ.BalanceBook orderby entry.Value descending select entry;
        string result = "";
        int place = 1;
        foreach (var entry in sorted)
        {
            result += $"{place}. {entry.Key.Split("/")[1]} with {entry.Value} {econ.Currencyname}\n";
            place++;
        }

        InteractivityExtension? interactivity = ctx.Client.GetInteractivity();
        var pages = interactivity.GeneratePagesInEmbed(result);

        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }
}