using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VerneMQ.Control.Hubs;
using VerneMQ.Control.Models;

namespace VerneMQ.Control.Services
{
	/// <summary>
	/// Implements updates for for the web interface.
	/// </summary>
	/// <seealso cref="Microsoft.Extensions.Hosting.IHostedService" />
	public class BackgroundUpdateService : IHostedService
	{
		private readonly ILogger logger;
		private readonly IServiceScopeFactory serviceScopeFactory;

		private Timer vmqTimer;

		/// <summary>
		/// Initializes a new instance of the <see cref="BackgroundUpdateService"/> class.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="serviceScopeFactory">The service scope factory.</param>
		public BackgroundUpdateService(ILogger<BackgroundUpdateService> logger, IServiceScopeFactory serviceScopeFactory)
		{
			this.logger = logger;
			this.serviceScopeFactory = serviceScopeFactory;
		}

		/// <summary>
		/// Triggered when the application host is ready to start the service.
		/// </summary>
		/// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
		/// <returns></returns>
		public Task StartAsync(CancellationToken cancellationToken)
		{
			var interval = TimeSpan.FromSeconds(2);
			vmqTimer = new Timer(OnVmqTimer, null, interval.GetAlignedInterval(), interval);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Triggered when the application host is performing a graceful shutdown.
		/// </summary>
		/// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
		/// <returns></returns>
		public Task StopAsync(CancellationToken cancellationToken)
		{
			vmqTimer?.Dispose();
			vmqTimer = null;
			return Task.CompletedTask;
		}

		private async void OnVmqTimer(object _)
		{
			try
			{
				using var httpClient = new HttpClient();
				using var scope = serviceScopeFactory.CreateScope();

				var hub = scope.ServiceProvider.GetRequiredService<IHubContext<WebHub>>();
				var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

				string url = configuration.GetValue("VerneMQ:Metrics", "http://localhost:8888/metrics");
				var urlMatch = Regex.Match(url, @"^(https?:\/\/)(.*)@(.*)$");
				if (urlMatch.Success)
				{
					httpClient.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(urlMatch.Groups[2].Value)));
					url = $"{urlMatch.Groups[1].Value}{urlMatch.Groups[3].Value}";
				}

				var res = await httpClient.GetAsync(url);
				if (!res.IsSuccessStatusCode)
					return;

				string content = await res.Content.ReadAsStringAsync();
				var matches = Regex.Matches(content, @"^([^#].+){.*} (.+)$", RegexOptions.Multiline);
				var metrics = new Dictionary<string, ulong>();
				foreach (Match match in matches)
				{
					if (!metrics.ContainsKey(match.Groups[1].Value))
						metrics.Add(match.Groups[1].Value, 0);

					metrics[match.Groups[1].Value] += ulong.Parse(match.Groups[2].Value);
				}

				var model = new VerneMQViewModel
				{
					SocketClose = metrics["socket_close"],
					SocketOpen = metrics["socket_open"],
					BytesReceived = metrics["bytes_received"],
					BytesSent = metrics["bytes_sent"],
					MessagesReceived = metrics["mqtt_publish_received"],
					MessagesSent = metrics["mqtt_publish_sent"],
					QueueIn = metrics["queue_message_in"],
					QueueOut = metrics["queue_message_out"],
					QueueDropped = metrics["queue_message_drop"],
					ClusterBytesReceived = metrics["cluster_bytes_received"],
					ClusterBytesSent = metrics["cluster_bytes_sent"],
					ClusterBytesDropped = metrics["cluster_bytes_dropped"],
					UsedMemoryBytes = metrics["vm_memory_total"],
					UptimeMilliseconds = metrics["system_wallclock"],
					RetainedMessages = metrics["retain_messages"],
					Subscriptions = metrics["router_subscriptions"]
				};

				await hub.Clients.Group(WebHub.Authenticated).SendAsync("UpdateVerneMQ", model);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"Updating VerneMQ Information failed: {ex.GetMessage()}");
			}
		}
	}
}
