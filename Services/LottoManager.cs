using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hitbot.Services;

public class LottoManager
{
    private readonly IDatabase db;
    public readonly int LottoDrawprice;
    public readonly int LottoTicketprice;

    public int Pot => (int) db.StringGet("lotto:pot");

    public LottoManager(ConnectionMultiplexer redisConnection)
    {
        db = redisConnection.GetDatabase();
        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            LottoTicketprice = int.Parse(config["lottoticketprice"]);
            LottoDrawprice = int.Parse(config["lottodrawprice"]);
        }
        else
        {
            throw new Exception("no config.json found");
        }
    }

    public void EnterLotto(string user)
    {
        db.SetAdd("lotto", user);
    }

    public void ClearLotto()
    {
        db.KeyDelete("lotto");
        db.KeyDelete("lotto:pot");
    }

    public void IncrPot(int by = 1)
    {
        db.StringIncrement("lotto:pot", by);
    }

    public List<string> LottoUsersAsList()
    {
        var redisValueList = db.SetMembers("lotto").ToList();
        return redisValueList.Select(entry => entry.ToString()).ToList();
    }
}