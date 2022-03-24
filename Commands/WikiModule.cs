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
}