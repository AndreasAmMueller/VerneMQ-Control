using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VerneMQ.Control.Database;
using VerneMQ.Control.Database.Entities;

namespace VerneMQ.Control.Services
{
	/// <summary>
	/// Initializes the application.
	/// </summary>
	/// <seealso cref="Microsoft.Extensions.Hosting.IHostedService" />
	public class InitService : IHostedService
	{
		private readonly ILogger logger;
		private readonly IServiceScopeFactory serviceScopeFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="InitService"/> class.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="serviceScopeFactory">The service scope factory.</param>
		public InitService(ILogger<InitService> logger, IServiceScopeFactory serviceScopeFactory)
		{
			this.logger = logger;
			this.serviceScopeFactory = serviceScopeFactory;
		}

		/// <summary>
		/// Gets a value indicating whether the initialization was successfull.
		/// </summary>
		public bool IsSuccess { get; private set; }

		/// <summary>
		/// Triggered when the application host is ready to start the service.
		/// </summary>
		/// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			try
			{
				using var scope = serviceScopeFactory.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
				var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

				if (await dbContext.MigrateAsync(logger, cancellationToken))
				{
					string username = configuration.GetValue<string>("Init:AdminName")?.Trim().ToLower();
					string password = configuration.GetValue<string>("Init:AdminPass")?.Trim();
					if (string.IsNullOrWhiteSpace(username))
						username = "admin";
					if (string.IsNullOrWhiteSpace(password))
						password = "admin";

					if (!await dbContext.Users.Where(u => u.IsEnabled).Where(u => u.IsAdmin).AnyAsync(cancellationToken))
					{
						var admin = await dbContext.Users
							.Where(u => u.Username == username)
							.FirstOrDefaultAsync(cancellationToken);
						if (admin == null)
						{
							admin = new WebUser
							{
								Id = dbContext.GetUserId(),
								Username = username,
								PasswordHash = PasswordHelper.HashPassword(password)
							};
							await dbContext.Users.AddAsync(admin, cancellationToken);
						}
						admin.IsEnabled = true;
						admin.IsAdmin = true;
						await dbContext.SaveChangesAsync(cancellationToken);
					}

					IsSuccess = true;
				}
			}
			catch (Exception ex)
			{
				logger.LogCritical(ex, $"Initialization failed: {ex.GetMessage()}");
				IsSuccess = false;
			}
		}

		/// <summary>
		/// Triggered when the application host is performing a graceful shutdown.
		/// </summary>
		/// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
		/// <returns></returns>
		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
