using System;

namespace ClusterClient.Clients
{
	class Replica
	{
		public string Url;
		public int ResponseCount = 0;
		public TimeSpan AverageResponseTime = TimeSpan.Zero;

		public Replica(string url)
		{
			Url = url;
		}

		public void UpdateResponseTime(TimeSpan newResponse)
		{
			AverageResponseTime = (AverageResponseTime * ResponseCount + newResponse) / ++ResponseCount;
		}
	}
}