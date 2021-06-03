using System;

namespace VerneMQ.Control.Models
{
	/// <summary>
	/// Represents a VerneMQ client session
	/// </summary>
	public class VmqClient
	{
		/// <summary>
		/// Gets or sets the name of the user.
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Gets or sets the client id.
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// Gets or sets the ip address.
		/// </summary>
		public string IpAddress { get; set; }

		/// <summary>
		/// Gets or sets the port.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Gets or sets the protocol version as number.
		/// </summary>
		public int Protocol { get; set; }

		/// <summary>
		/// Gets the protocol version as string.
		/// </summary>
		public string ProtocolVersion => Protocol switch
		{
			3 => "3.1.0",
			4 => "3.1.1",
			5 => "5.0.0",
			_ => "unknown"
		};

		/// <summary>
		/// Gets or sets the connection status.
		/// </summary>
		public string Status { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the client started with a clean session.
		/// </summary>
		public bool CleanSession { get; set; }

		/// <summary>
		/// Gets or sets the session start time in milliseconds.
		/// </summary>
		public ulong SessionStart { get; set; }

		/// <summary>
		/// Gets the session start time.
		/// </summary>
		public DateTime SessionStartTime => DateTime.UnixEpoch.AddMilliseconds(SessionStart);
	}
}
