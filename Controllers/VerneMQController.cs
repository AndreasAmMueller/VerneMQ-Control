using System.Linq;
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
			if (metrics?.Any() != true)
				return View(model);

			model.SocketClose = metrics["socket_close"];
			model.SocketOpen = metrics["socket_open"];
			model.BytesReceived = metrics["bytes_received"];
			model.BytesSent = metrics["bytes_sent"];
			model.MessagesReceived = metrics["mqtt_publish_received"];
			model.MessagesSent = metrics["mqtt_publish_sent"];
			model.QueueIn = metrics["queue_message_in"];
			model.QueueOut = metrics["queue_message_out"];
			model.QueueDropped = metrics["queue_message_drop"];
			model.ClusterBytesReceived = metrics["cluster_bytes_received"];
			model.ClusterBytesSent = metrics["cluster_bytes_sent"];
			model.ClusterBytesDropped = metrics["cluster_bytes_dropped"];
			model.UsedMemoryBytes = metrics["vm_memory_total"];
			model.UptimeMilliseconds = metrics["system_wallclock"];
			model.RetainedMessages = metrics["retain_messages"];
			model.Subscriptions = metrics["router_subscriptions"];

			return View(model);
		}
	}
}
