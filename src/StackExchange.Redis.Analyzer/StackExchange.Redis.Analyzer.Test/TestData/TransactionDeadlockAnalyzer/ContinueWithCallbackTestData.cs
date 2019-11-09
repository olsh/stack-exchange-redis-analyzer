using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Redis.Analyzer.Test.TestData
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var redis = await ConnectionMultiplexer.ConnectAsync("localhost")
                            .ConfigureAwait(false);
            var db = redis.GetDatabase();

            await Task.WhenAll(Task.CompletedTask)
                .ContinueWith(
                    async task =>
                        {
                            var transaction = db.CreateTransaction();

                            var tasks = new List<Task> { transaction.SetAddAsync("test", "test") };

                            await transaction.ExecuteAsync().ConfigureAwait(false);
                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        })
                .ConfigureAwait(false);
        }
    }
}
