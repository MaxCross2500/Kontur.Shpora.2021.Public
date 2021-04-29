using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class SmartClusterClient : ClusterClientBase
    {
        private readonly Replica[] replicas;
        public SmartClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            replicas = replicaAddresses.Select(url => new Replica(url)).ToArray();
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var orderedReplicas = replicas.OrderBy(replica => replica.AverageResponseTime).ToArray();
            // var orderedReplicas = replicas;
            
            var replicasLeft = orderedReplicas.Length;
            var timeLeft = timeout;

            var webRequestTasks = new HashSet<Task<string>>();
            
            foreach (var replica in orderedReplicas)
            {
                var stopwatch = Stopwatch.StartNew();
                
                webRequestTasks.Add(ProcessReplicaRequestAsync(replica, query));
                var anyRequestTask = Task.WhenAny(webRequestTasks);
                
                await Task.WhenAny(anyRequestTask, Task.Delay(timeLeft / replicasLeft--));
                stopwatch.Stop();
                timeLeft -= stopwatch.Elapsed;
                
                if (!anyRequestTask.IsCompleted) continue;
                
                webRequestTasks.Remove(anyRequestTask.Result);
                if (anyRequestTask.Result.IsFaulted) continue;

                return anyRequestTask.Result.Result;
            }
            
            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}
