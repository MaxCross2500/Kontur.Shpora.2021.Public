using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class ParallelClusterClient : ClusterClientBase
    {
        public ParallelClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var tasks = ReplicaAddresses.Select(async address =>
            {
                var webRequest = CreateRequest($"{address}?query={query}");
                return await ProcessRequestAsync(webRequest);
            });
            var resultTask = WhenAnyCompleted(tasks);
            await Task.WhenAny(resultTask, Task.Delay(timeout));

            if (!resultTask.IsCompleted) throw new TimeoutException();
            return resultTask.Result.Result;
        }

        private static async Task<Task<T>> WhenAnyCompleted<T>(IEnumerable<Task<T>> tasks)
        {
            var tasksCollection = tasks.ToHashSet();
            while (tasksCollection.Any())
            {
                var completed = await Task.WhenAny(tasksCollection);
                if (!completed.IsFaulted) return completed;
                tasksCollection.Remove(completed);
            }

            throw new Exception("All tasks faulted");
        }

        protected override ILog Log => LogManager.GetLogger(typeof(ParallelClusterClient));
    }
}
