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
}