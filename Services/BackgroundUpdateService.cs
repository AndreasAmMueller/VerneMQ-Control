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
					Subscriptions = metrics["router_subscriptions"],
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
