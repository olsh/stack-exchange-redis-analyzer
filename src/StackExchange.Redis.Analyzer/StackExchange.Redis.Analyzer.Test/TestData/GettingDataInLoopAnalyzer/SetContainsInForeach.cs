using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            var key = "key";

            // This should not be reported
            var value = db.SetContains(key, "one");
            foreach (var setValue in new[] { "one", "two" })
            {
                var value = db.SetContains(key, setValue);
            }
        }
    }
}
