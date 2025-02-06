using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VerneMQ.Control.Hubs;
using VerneMQ.Control.Models;
using VerneMQ.Control.Utils;

namespace VerneMQ.Control.Services
{
	/// <summary>
	/// Implements updates for for the web interface.
	/// </summary>
	/// <seealso cref="IHostedService" />
	public class BackgroundUpdateService : IHostedService
	{
		private readonly ILogger logger;
		private readonly IServiceScopeFactory serviceScopeFactory;

		private Timer vmqTimer;
		private bool errorReported;

		private SemaphoreSlim timerLock;

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
			var interval = TimeSpan.FromSeconds(5);

			timerLock = new SemaphoreSlim(1, 1);
			vmqTimer = new Timer(OnVmqTimer, null, interval.GetAlignedIntervalUtc(), interval);

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

			timerLock?.Dispose();
			timerLock = null;

			return Task.CompletedTask;
		}

		private async void OnVmqTimer(object _)
		{
			try
			{
				if (!timerLock.Wait(0))
					return;

				using var scope = serviceScopeFactory.CreateScope();

				var hub = scope.ServiceProvider.GetRequiredService<IHubContext<WebHub>>();
				var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

				var metrics = await VmqHelper.GetMetrics(configuration.GetValue("VerneMQ:Metrics", "http://localhost:8888/metrics"));
				if (metrics == null)
					return;

				var model = new VerneMQViewModel
				{
					SocketClose = metrics.TryGetValue("socket_close", out ulong socketClose) ? socketClose : 0,
					SocketOpen = metrics.TryGetValue("socket_open", out ulong socketOpen) ? socketOpen : 0,
					BytesReceived = metrics.TryGetValue("bytes_received", out ulong bytesReceived) ? bytesReceived : 0,
					BytesSent = metrics.TryGetValue("bytes_sent", out ulong bytesSent) ? bytesSent : 0,
					MessagesReceived = metrics.TryGetValue("mqtt_publish_received", out ulong messagesReceived) ? messagesReceived : 0,
					MessagesSent = metrics.TryGetValue("mqtt_publish_sent", out ulong messagesSent) ? messagesSent : 0,
					QueueIn = metrics.TryGetValue("queue_message_in", out ulong queueIn) ? queueIn : 0,
					QueueOut = metrics.TryGetValue("queue_message_out", out ulong queueOut) ? queueOut : 0,
					QueueDropped = metrics.TryGetValue("queue_message_drop", out ulong queueDropped) ? queueDropped : 0,
					ClusterBytesReceived = metrics.TryGetValue("cluster_bytes_received", out ulong clusterBytesReceived) ? clusterBytesReceived : 0,
					ClusterBytesSent = metrics.TryGetValue("cluster_bytes_sent", out ulong clusterBytesSent) ? clusterBytesSent : 0,
					ClusterBytesDropped = metrics.TryGetValue("cluster_bytes_dropped", out ulong clusterBytesDropped) ? clusterBytesDropped : 0,
					UsedMemoryBytes = metrics.TryGetValue("vm_memory_total", out ulong usedMemoryBytes) ? usedMemoryBytes : 0,
					UptimeMilliseconds = metrics.TryGetValue("system_wallclock", out ulong uptimeMilliseconds) ? uptimeMilliseconds : 0,
					RetainedMessages = metrics.TryGetValue("retain_messages", out ulong retainedMessages) ? retainedMessages : 0,
					Subscriptions = metrics.TryGetValue("router_subscriptions", out ulong subscriptions) ? subscriptions : 0,

					Clients = await VmqHelper.GetClients(configuration.GetValue("VerneMQ:Admin", "/vernemq/bin/vmq-admin"), logger)
				};

				await hub.Clients.Group(WebHub.Authenticated).SendAsync("UpdateVerneMQ", model);
				errorReported = false;
			}
			catch (Exception ex)
			{
				if (!errorReported)
					logger.LogError(ex, $"Updating VerneMQ Information failed: {ex.GetMessage()}");

				errorReported = true;
			}
			finally
			{
				timerLock?.Release();
			}
		}
	}
}
