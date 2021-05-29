using System;

namespace VerneMQ.Control.Models
{
	/// <summary>
	/// Transfer class for VerneMQ status information.
	/// </summary>
	public class VerneMQViewModel
	{
		#region From Metrics

		/// <summary>
		/// Gets or sets the number of open sockets.
		/// </summary>
		public ulong SocketOpen { get; set; }

		/// <summary>
		/// Gets or sets the number of closed sockets.
		/// </summary>
		public ulong SocketClose { get; set; }

		/// <summary>
		/// Gets or sets the bytes received.
		/// </summary>
		public ulong BytesReceived { get; set; }

		/// <summary>
		/// Gets or sets the bytes sent.
		/// </summary>
		public ulong BytesSent { get; set; }

		/// <summary>
		/// Gets or sets the messages received.
		/// </summary>
		public ulong MessagesReceived { get; set; }

		/// <summary>
		/// Gets or sets the messages sent.
		/// </summary>
		public ulong MessagesSent { get; set; }

		/// <summary>
		/// Gets or sets the number of messages enqueued.
		/// </summary>
		public ulong QueueIn { get; set; }

		/// <summary>
		/// Gets or sets the number of messages dequeued.
		/// </summary>
		public ulong QueueOut { get; set; }

		/// <summary>
		/// Gets or sets the number of messages dropped from queue.
		/// </summary>
		public ulong QueueDropped { get; set; }

		/// <summary>
		/// Gets or sets the bytes received from the cluster.
		/// </summary>
		public ulong ClusterBytesReceived { get; set; }

		/// <summary>
		/// Gets or sets the bytes sent to the cluster.
		/// </summary>
		public ulong ClusterBytesSent { get; set; }

		/// <summary>
		/// Gets or sets the bytes dropped by the cluster.
		/// </summary>
		public ulong ClusterBytesDropped { get; set; }

		/// <summary>
		/// Gets or sets the used memory in bytes.
		/// </summary>
		public ulong UsedMemoryBytes { get; set; }

		/// <summary>
		/// Gets or sets the uptime in milliseconds.
		/// </summary>
		public ulong UptimeMilliseconds { get; set; }

		/// <summary>
		/// Gets or sets the retained messages.
		/// </summary>
		public ulong RetainedMessages { get; set; }

		/// <summary>
		/// Gets or sets the subscriptions.
		/// </summary>
		public ulong Subscriptions { get; set; }

		#endregion From Metrics

		#region Display

		/// <summary>
		/// Gets the clients online.
		/// </summary>
		public int ClientsOnline => (int)(SocketOpen - SocketClose);

		/// <summary>
		/// Gets the data received.
		/// </summary>
		public string DataReceived => AutoSize(BytesReceived);

		/// <summary>
		/// Gets the data sent.
		/// </summary>
		public string DataSent => AutoSize(BytesSent);

		/// <summary>
		/// Gets the cluster data received.
		/// </summary>
		public string ClusterReceived => AutoSize(ClusterBytesReceived);

		/// <summary>
		/// Gets the cluster data sent.
		/// </summary>
		public string ClusterSent => AutoSize(ClusterBytesSent);

		/// <summary>
		/// Gets the cluster data dropped.
		/// </summary>
		public string ClusterDropped => AutoSize(ClusterBytesDropped);

		/// <summary>
		/// Gets the used memory.
		/// </summary>
		public string UsedMemory => AutoSize(UsedMemoryBytes);

		/// <summary>
		/// Gets the uptime.
		/// </summary>
		public string Uptime => TimeSpan.FromMilliseconds(UptimeMilliseconds).ToShortString();

		#endregion Display

		private static string AutoSize(ulong bytes)
		{
			ulong scale = 1024 * (ulong)(1024 * 1024 * 1024);

			if (bytes > scale)
				return ((decimal)bytes / scale).ToString("N2") + " TB";

			scale /= 1024;
			if (bytes > scale)
				return ((decimal)bytes / scale).ToString("N2") + " GB";

			scale /= 1024;
			if (bytes > scale)
				return ((decimal)bytes / scale).ToString("N2") + " MB";

			scale /= 1024;
			if (bytes > scale)
				return ((decimal)bytes / scale).ToString("N2") + " KB";

			return bytes + " B";
		}
	}
}
