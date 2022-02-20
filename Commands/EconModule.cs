using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Hitbot.Commands;

public class EconModule : BaseCommandModule
{
    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        WriteBalances();
        return Task.Delay(0);
    }

    [Command("register")]
    [Description("Add your name to the books and get a starting balance")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        BalanceBook ??= new Dictionary<string, int>();
        DiscordMember? caller = ctx.Member;
        if (BalanceBook.ContainsKey(GetBalancebookString(caller)))
            await ctx.RespondAsync("You are already registered in this server.");
        else
        {
            BalanceBook.Add(GetBalancebookString(caller), int.Parse(Program.config["startingamount"]));
            await ctx.RespondAsync("Registered :)");
        }
    }

    [Command("balance")]
    [Description("Get your current balance.")]
    public async Task BalanceCommand(CommandContext ctx)
    {
        if (BalanceBook.ContainsKey(GetBalancebookString(ctx.Member)))
            await ctx.RespondAsync(
                $"Your balance is {BalanceBook[GetBalancebookString(ctx.Member)]} {Currencyname}.");
    }

    [Command("pay")]
    [Description("Pay currency to another user.")]
    public async Task PayCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        var caller = ctx.Member;
        if (caller.Equals(recipient))
        {
            await ctx.RespondAsync("You can't pay yourself!");
            return;
        }

        if (!BalanceBook.ContainsKey(GetBalancebookString(caller)))
        {
            await ctx.RespondAsync("You aren't registered. Please do so with ~register.");
            return;
        }

        if (!BalanceBook.ContainsKey(GetBalancebookString(recipient)))
        {
            await ctx.RespondAsync("Registering recipient...");
            BalanceBook.Add(GetBalancebookString(recipient), int.Parse(Program.config["startingamount"]));
        }

        if (BalanceBook[GetBalancebookString(caller)] < amount)
        {
            await ctx.RespondAsync("Insufficient funds.");
            return;
        }

        BalanceBook[GetBalancebookString(caller)] -= amount;
        BalanceBook[GetBalancebookString(recipient)] += amount;
        await ctx.RespondAsync(
            $"Paid {amount} {Currencyname} to {recipient.Nickname} (you now have {BalanceBook[GetBalancebookString(caller)]}, " +
            $"they have {BalanceBook[GetBalancebookString(recipient)]})");
    }

    //overload for backwards params
    [Command("pay")]
    [Description("Pay someone else a certain amount.")]
    public async Task PayCommand(CommandContext ctx, int amount, DiscordMember recipient)
    {
        await PayCommand(ctx, recipient, amount);
    }

    [Command("print")]
    [Description("Owner only. Prints new currency and gives to specified user.")]
    [RequireGuild]
    [RequireOwner]
    public async Task PrintCommand(CommandContext ctx, DiscordMember recipient, int amount)
    {
        BalanceBook[GetBalancebookString(recipient)] += amount;
        await ctx.RespondAsync($"{amount} new currency given to {recipient.Nickname}");
    }

    [Command("leaderboard")]
    [Description("Display a list of registered users sorted by descending balance.")]
    public async Task LeaderboardCommand(CommandContext ctx)
    {
        var sorted = from entry in BalanceBook orderby entry.Value descending select entry;
        string result = "";
        int place = 1;
        foreach (var entry in sorted)
        {
            result += $"{place}. {entry.Key.Split("/")[1]} with {entry.Value}\n";
            place++;
        }
        
        var interactivity = ctx.Client.GetInteractivity();
        var pages = interactivity.GeneratePagesInEmbed(result);

        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
    }

    public static Dictionary<string, int> BalanceBook;
    public static string Currencyname;

    private static void WriteBalances()
    {
        // JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
        // {
        //     TypeNameHandling = TypeNameHandling.All
        // };
        File.WriteAllText("balances.json", JsonConvert.SerializeObject(BalanceBook));
    }

    private static string GetBalancebookString(DiscordMember member)
    {
        return member.Id + "/" + member.Username + "#" + member.Discriminator;
    }

    private static string GetBalancebookId(DiscordMember member)
    {
        return GetBalancebookString(member).Split("/")[0];
    }
}