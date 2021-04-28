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
                var webRequest = CreateRequest($"{replica.Url}?query={query}");
                
                var stopwatch = Stopwatch.StartNew();
                webRequestTasks.Add(TryProcessRequestAsync(webRequest));
                var webRequestTask = Task.WhenAny(webRequestTasks);
                
                await Task.WhenAny(webRequestTask, Task.Delay(timeLeft / replicasLeft--));
                stopwatch.Stop();
                timeLeft -= stopwatch.Elapsed;
                if (webRequestTask.Status != TaskStatus.RanToCompletion || webRequestTask.Result.Result == null)
                {
                    if (webRequestTask.Status == TaskStatus.RanToCompletion && webRequestTask.Result.Result == null)
                        webRequestTasks.Remove(webRequestTask.Result);
                    continue;
                }

                webRequestTasks.Remove(webRequestTask.Result);
                replica.UpdateResponseTime(stopwatch.Elapsed);
                
                return webRequestTask.Result.Result;
            }
            
            throw new TimeoutException();
        }

        protected async Task<string> TryProcessRequestAsync(WebRequest request)
        {
            try
            {
                return await ProcessRequestAsync(request);
            }
            catch
            {
                return null;
            }
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}
