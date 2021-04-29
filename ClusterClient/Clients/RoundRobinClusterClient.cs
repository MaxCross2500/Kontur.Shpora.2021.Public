using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RoundRobinClusterClient : ClusterClientBase
    {
        private readonly Replica[] replicas;
        public RoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            replicas = replicaAddresses.Select(url => new Replica(url)).ToArray();
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var orderedReplicas = replicas.OrderBy(replica => replica.AverageResponseTime).ToArray();
            // var orderedReplicas = replicas;
            
            var replicasLeft = orderedReplicas.Length;
            var timeLeft = timeout;
            
            foreach (var replica in orderedReplicas)
            {
                var stopwatch = Stopwatch.StartNew();
                var webRequestTask = ProcessReplicaRequestAsync(replica, query);
                
                await Task.WhenAny(webRequestTask, Task.Delay(timeLeft / replicasLeft--));
                stopwatch.Stop();
                timeLeft -= stopwatch.Elapsed;
                if (!webRequestTask.IsCompletedSuccessfully) continue;
                
                return webRequestTask.Result;
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RoundRobinClusterClient));
    }
}
