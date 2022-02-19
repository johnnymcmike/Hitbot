using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Hitbot.Commands;

public class EconModule : BaseCommandModule
{
    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        WriteBalances();
        return Task.Delay(0);
    }

    [Command("greet")]
    public async Task GreetCommand(CommandContext ctx)
    {
        await ctx.RespondAsync("Greetings");
    }

    [Command("register")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        DiscordMember? user = ctx.Member;
        if (Balances.ContainsKey(user))
            await ctx.RespondAsync("You are already registered in this server.");
        else
        {
            Balances.Add(user, 10);
            await ctx.RespondAsync("Registered");
        }
    }

    public static Dictionary<DiscordMember, int> Balances;

    public static void WriteBalances()
    {
        File.WriteAllText("balances.json",JsonConvert.SerializeObject(Balances));
    }
}