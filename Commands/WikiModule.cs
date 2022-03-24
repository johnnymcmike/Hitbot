﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
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
        var wa = JsonConvert.DeserializeObject<List<string>>(content);
        string? b = wa[3];
        if (!string.IsNullOrEmpty(b))
        {
            b = b.Substring(b.IndexOf("http"));
            b = b.Substring(0, b.IndexOf("\""));
            await ctx.RespondAsync(b);
            return;
        }

        await ctx.RespondAsync("not found");
    }
}