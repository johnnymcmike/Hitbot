using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Hitbot.Commands;

public class EconModule : BaseCommandModule
{
    [Command("greet")]
    public async Task GreetCommand(CommandContext ctx)
    {
        await ctx.RespondAsync("Greetings");
    }

    [Command("register")]
    public async Task RegisterCommand(CommandContext ctx)
    {
        var user = ctx.Member;
        
    }
}