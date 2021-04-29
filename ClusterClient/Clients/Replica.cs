using System;
using System.Collections.Generic;
using System.Threading;

namespace ClusterClient.Clients
{
	public class Replica
	{
		public readonly string Url;

		public TimeSpan AverageResponseTime =>
			responseTimes.Count > 0 ? totalResponseTimeInWindow / responseTimes.Count : TimeSpan.Zero;

		private readonly int window;
		private readonly Queue<TimeSpan> responseTimes = new();
		private TimeSpan totalResponseTimeInWindow = TimeSpan.Zero;

		public Replica(string url, int window = 10)
		{
			Url = url;
			this.window = window;
		}

		public void UpdateResponseTime(TimeSpan newResponse)
		{
			lock (responseTimes)
			{
				responseTimes.Enqueue(newResponse);
				totalResponseTimeInWindow += newResponse;
				while (responseTimes.Count > window) totalResponseTimeInWindow -= responseTimes.Dequeue();
			}
		}

		public void UpdateResponseTimeLocked(TimeSpan newResponse, ReaderWriterLockSlim rwlock)
		{
			rwlock.EnterWriteLock();
			responseTimes.Enqueue(newResponse);
			totalResponseTimeInWindow += newResponse;
			while (responseTimes.Count > window) totalResponseTimeInWindow -= responseTimes.Dequeue();
			rwlock.ExitWriteLock();
		}
	}
}