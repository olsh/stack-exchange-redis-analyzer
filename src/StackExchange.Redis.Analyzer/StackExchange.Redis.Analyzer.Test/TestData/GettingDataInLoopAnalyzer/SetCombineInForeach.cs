using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();
            foreach (var setValue in new[] { "one", "two" })
            {
                var value = db.SetCombine(SetOperation.Intersect, "key", setValue);
            }
        }
    }
}
