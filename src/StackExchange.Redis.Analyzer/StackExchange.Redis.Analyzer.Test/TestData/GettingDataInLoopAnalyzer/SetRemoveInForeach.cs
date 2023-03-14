using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            var value = "deleted";
            foreach (var setKey in new[] { "one", "two" })
            {
                db.SetRemove(setKey, value);
            }
        }
    }
}
