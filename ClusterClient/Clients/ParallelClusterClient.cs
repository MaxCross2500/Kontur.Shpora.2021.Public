using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var resultTask = WhenAnyCompleted(tasks.ToHashSet());
            await Task.WhenAny(resultTask, Task.Delay(timeout));

            if (!resultTask.IsCompleted) throw new TimeoutException();
            return resultTask.Result.Result;
        }

        protected async Task<Task<T>> WhenAnyCompleted<T>(HashSet<Task<T>> tasks)
        {
            while (tasks.Any())
            {
                var completed = await Task.WhenAny(tasks);
                if (completed.Status == TaskStatus.RanToCompletion) return completed;
                tasks.Remove(completed);
            }

            throw new Exception("Nothing was completed, and I don't know how to fault task properly");
        }

        protected override ILog Log => LogManager.GetLogger(typeof(ParallelClusterClient));
    }
}
