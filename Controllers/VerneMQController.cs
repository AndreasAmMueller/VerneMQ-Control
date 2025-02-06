using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerneMQ.Control.Models;
using VerneMQ.Control.Utils;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the status view.
	/// </summary>
	/// <seealso cref="Controller" />
	[Authorize]
	public class VerneMQController : Controller
	{
		private readonly ILogger logger;
		private readonly IConfiguration configuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="VerneMQController"/> class.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="configuration">The configuration.</param>
		public VerneMQController(ILogger<VerneMQController> logger, IConfiguration configuration)
		{
			this.logger = logger;
			this.configuration = configuration;
		}

		/// <summary>
		/// Shows the status view.
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> Index()
		{
			ViewData["Title"] = "Status";

			var model = new VerneMQViewModel
			{
				Clients = await VmqHelper.GetClients(configuration.GetValue("VerneMQ:Admin", "/vernemq/bin/vmq-admin"), logger, HttpContext.RequestAborted)
			};

			var metrics = await VmqHelper.GetMetrics(configuration.GetValue("VerneMQ:Metrics", "http://localhost:8888/metrics"), logger, HttpContext.RequestAborted);
			if (metrics?.Count > 0)
			{
				model.SocketClose = metrics.TryGetValue("socket_close", out ulong socketClose) ? socketClose : 0;
				model.SocketOpen = metrics.TryGetValue("socket_open", out ulong socketOpen) ? socketOpen : 0;
				model.BytesReceived = metrics.TryGetValue("bytes_received", out ulong bytesReceived) ? bytesReceived : 0;
				model.BytesSent = metrics.TryGetValue("bytes_sent", out ulong bytesSent) ? bytesSent : 0;
				model.MessagesReceived = metrics.TryGetValue("mqtt_publish_received", out ulong messagesReceived) ? messagesReceived : 0;
				model.MessagesSent = metrics.TryGetValue("mqtt_publish_sent", out ulong messagesSent) ? messagesSent : 0;
				model.QueueIn = metrics.TryGetValue("queue_message_in", out ulong queueIn) ? queueIn : 0;
				model.QueueOut = metrics.TryGetValue("queue_message_out", out ulong queueOut) ? queueOut : 0;
				model.QueueDropped = metrics.TryGetValue("queue_message_drop", out ulong queueDropped) ? queueDropped : 0;
				model.ClusterBytesReceived = metrics.TryGetValue("cluster_bytes_received", out ulong clusterBytesReceived) ? clusterBytesReceived : 0;
				model.ClusterBytesSent = metrics.TryGetValue("cluster_bytes_sent", out ulong clusterBytesSent) ? clusterBytesSent : 0;
				model.ClusterBytesDropped = metrics.TryGetValue("cluster_bytes_dropped", out ulong clusterBytesDropped) ? clusterBytesDropped : 0;
				model.UsedMemoryBytes = metrics.TryGetValue("vm_memory_total", out ulong usedMemoryBytes) ? usedMemoryBytes : 0;
				model.UptimeMilliseconds = metrics.TryGetValue("system_wallclock", out ulong uptimeMilliseconds) ? uptimeMilliseconds : 0;
				model.RetainedMessages = metrics.TryGetValue("retain_messages", out ulong retainedMessages) ? retainedMessages : 0;
				model.Subscriptions = metrics.TryGetValue("router_subscriptions", out ulong subscriptions) ? subscriptions : 0;
			}

			return View(model);
		}
	}
}
