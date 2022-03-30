using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json.Linq;

namespace Hitbot.Commands;

[Group("wiki")]
public class WikiModule : BaseCommandModule
{
    private readonly HttpClient _http;

    public WikiModule(HttpClient ht)
    {
        _http = ht;
    }

    [Command("random")]
    public async Task RandomWikiArticleTask(CommandContext ctx)
    {
        var response = await _http.GetAsync("https://en.wikipedia.org/api/rest_v1/page/random/summary");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string? finalresponse = Convert.ToString(JObject.Parse(content)["content_urls"]?["desktop"]?["page"]);
        if (finalresponse is null)
        {
            await ctx.RespondAsync("this should never happen");
            return;
        }

        await ctx.RespondAsync(finalresponse);
    }

    [Command("search")]
    public async Task SearchWikipediaTask(CommandContext ctx, [RemainingText] string searchstring)
    {
        var response =
            await _http.GetAsync(
                $"https://en.wikipedia.org/w/api.php?action=opensearch&search={searchstring}&limit=1&namespace=0&format=json");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string wa = content.Split('[')[4].Substring(1);
        Console.WriteLine(wa);
        if (wa.IndexOf("\"") == -1)
        {
            await ctx.RespondAsync("not found");
            return;
        }

        wa = wa.Substring(0, wa.IndexOf("\""));
        await ctx.RespondAsync(wa);
    }
}

public class APIStuffModule : BaseCommandModule
{
    private readonly HttpClient _http;

    public APIStuffModule(HttpClient ht)
    {
        _http = ht;
    }

    [Command("dadjoke")]
    public async Task DadJokeCommand(CommandContext ctx)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Get, "https://icanhazdadjoke.com/"))
        {
            request.Headers.Add("Accept", "text/plain");
            var response = await _http.SendAsync(request);
            await ctx.Channel.SendMessageAsync(await response.Content.ReadAsStringAsync());
        }
    }

    [Command("qr")]
    [Description(
        "Attempts to generate and then embed a QR code based on the link you send in this command. Only lasts for 24 hours.")]
    public async Task QrCommand(CommandContext ctx, string url)
    {
        await ctx.RespondAsync($"https://qrtag.net/api/qr.png?url={url}");
    }
}