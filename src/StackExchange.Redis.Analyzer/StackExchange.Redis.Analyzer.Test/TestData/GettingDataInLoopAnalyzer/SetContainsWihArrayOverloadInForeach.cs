using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            foreach (var key in new[] { "one", "two" })
            {
                var value = db.SetContains(key, new []{ "1", "2" }, CommandFlags.None);
            }
        }
    }
}
