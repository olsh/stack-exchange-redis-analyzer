using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();

            // This should not be reported
            var value = db.SetContains("test", "1");
            foreach (var key in new[] { "one", "two" })
            {
                var value = db.SetContains(key, "1");
            }
        }
    }
}
