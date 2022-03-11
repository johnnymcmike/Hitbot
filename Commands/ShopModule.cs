using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hitbot.Services;

namespace Hitbot.Commands;

public class ShopModule : BaseCommandModule
{
    private EconManager Econ { get; }

    public ShopModule(EconManager eco)
    {
        Econ = eco;
    }

    [Command("emojishop")]
    [Description(
        "For 200, set the emoji currently in the :botemoji: slot to whatever image you want. Attach the image in the same message as your command.")]
    public async Task EmojiBuyTask(CommandContext ctx)
    {
        string callerstring = Program.GetBalancebookString(ctx.Member);
        if (Econ.BookGet(callerstring) < 2)
        {
            await ctx.RespondAsync("Insufficient funds.");
            return;
        }

        var attach = ctx.Message.Attachments;
        if (attach.Count != 1)
        {
            await ctx.RespondAsync("Invalid number of attachments. Please send only one image.");
            return;
        }

        var myAttachment = attach[0];

        if (myAttachment.MediaType is not ("image/png" or "image/jpeg" or "image/jpg"))
        {
            await ctx.RespondAsync(".PNG, .JPEG, and .JPG image files only.");
            return;
        }

        if (myAttachment.FileSize > 250000) //not positive on whether this is right so it's a lowball on purpose
        {
            await ctx.RespondAsync("File too big.");
            return;
        }

        //if all the above checks passed:
        Stream ms;
        using (var http = new HttpClient())
        {
            ms = await (await http.GetAsync(myAttachment.Url)).Content.ReadAsStreamAsync();
        }

        await ctx.Guild.DeleteEmojiAsync(
            DiscordEmoji.FromName(ctx.Client, "botemoji") as DiscordGuildEmoji); //TODO: seems bad
        await ctx.Guild.CreateEmojiAsync("botemoji", ms);
        await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":botemoji:")} :)");
        Econ.BookDecr(callerstring, 200);
    }
}