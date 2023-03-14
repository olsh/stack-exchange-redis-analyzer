using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            for (int i = 0; i < 5; i++)
            {
                const var demo = "constant";
                var value = await db.StringGetAsync(demo);
            }
        }
    }
}
