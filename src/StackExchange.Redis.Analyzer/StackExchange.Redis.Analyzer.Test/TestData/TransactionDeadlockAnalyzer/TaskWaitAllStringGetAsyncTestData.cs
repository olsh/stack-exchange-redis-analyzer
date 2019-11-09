using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            var tran = db.CreateTransaction();
            Task.WaitAll(tran.StringGetAsync("test"));
            await tran.ExecuteAsync();
        }
    }
}
