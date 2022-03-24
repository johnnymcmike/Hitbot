using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Emzi0767.Utilities;

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
        var content = response.Content.ToDictionary();
        foreach (var VARIABLE in content) Console.WriteLine(VARIABLE.Key);
    }
}