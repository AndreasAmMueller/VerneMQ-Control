using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerneMQ.Control.Models;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the status view.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
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
			var model = new VerneMQViewModel();
			ViewData["Title"] = "Status";
			try
			{
				using var httpClient = new HttpClient();
				string url = configuration.GetValue("VerneMQ:Metrics", "http://localhost:8888/metrics");
				var urlMatch = Regex.Match(url, @"^(https?:\/\/)(.*)@(.*)$");
				if (urlMatch.Success)
				{
					httpClient.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(urlMatch.Groups[2].Value)));
					url = $"{urlMatch.Groups[1].Value}{urlMatch.Groups[3].Value}";
				}

				var res = await httpClient.GetAsync(url);
				if (!res.IsSuccessStatusCode)
					return View(model);

				string content = await res.Content.ReadAsStringAsync();
				var matches = Regex.Matches(content, @"^([^#].+){.*} (.+)$", RegexOptions.Multiline);
				var metrics = new Dictionary<string, ulong>();
				foreach (Match match in matches)
				{
					if (!metrics.ContainsKey(match.Groups[1].Value))
						metrics.Add(match.Groups[1].Value, 0);

					metrics[match.Groups[1].Value] += ulong.Parse(match.Groups[2].Value);
				}

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
			catch (Exception ex)
			{
				logger.LogError(ex, $"Loading metrics failed: {ex.GetMessage()}");
				return View(model);
			}
		}
	}
}
