using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Hitbot.Services;

public class EconManager
{
    public Dictionary<string, int> BalanceBook;
    public string Currencyname;
    public int startingamount;

    public EconManager()
    {
        if (File.Exists("balances.json"))
        {
            BalanceBook =
                JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText("balances.json"))!;
        }
        else
        {
            BalanceBook = new Dictionary<string, int>();
        }

        if (File.Exists("config.json"))
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText("config.json"))!;
            Currencyname = config["currencyname"];
            startingamount = int.Parse(config["startingamount"]);
        }
        else
        {
            throw new Exception("no config.json found");
        }
    }

    public void WriteBalances()
    {
        // JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
        // {
        //     TypeNameHandling = TypeNameHandling.All
        // };
        File.WriteAllText("balances.json", JsonConvert.SerializeObject(BalanceBook));
    }

    public string GetBalancebookString(DiscordMember member)
    {
        return member.Id + "/" + member.Username + "#" + member.Discriminator;
    }
}