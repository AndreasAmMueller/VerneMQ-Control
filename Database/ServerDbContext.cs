using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Common.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VerneMQ.Control.Database.Entities;

namespace VerneMQ.Control.Database
{
	/// <summary>
	/// Implements the database connection.
	/// </summary>
	/// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
	public class ServerDbContext : DbContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServerDbContext"/> class.
		/// </summary>
		/// <param name="options">The options.</param>
		public ServerDbContext(DbContextOptions<ServerDbContext> options)
			: base(options)
		{ }

		/// <summary>
		/// Gets or sets the MQTT permissions.
		/// </summary>
		public DbSet<MqttPermission> MqttPermissions { get; protected set; }

		/// <summary>
		/// Gets or sets the MQTT users.
		/// </summary>
		public DbSet<MqttUser> MqttUsers { get; protected set; }

		/// <summary>
		/// Gets or sets the (web-)users.
		/// </summary>
		public DbSet<WebUser> Users { get; protected set; }

		/// <inheritdoc/>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.ApplySnakeCase();
			builder.ApplyIndexAttributes();

			builder.Entity<MqttPermission>()
				.HasKey(e => new { e.UserId, e.Topic });
		}

		internal int GetMqttUserId()
		{
			var rand = new Random();
			int id;
			do
			{
				// capacity of 998_999_999 users.
				// includes the lower limit and excludes the upper limit.
				id = rand.Next(1_000_000, 1_000_000_000);
			}
			while (MqttUsers.Any(u => u.Id == id));
			return id;
		}

		internal int GetUserId()
		{
			var rand = new Random();
			int id;
			do
			{
				// capacity of 998_999_999 users.
				// includes the lower limit and excludes the upper limit.
				id = rand.Next(1_000_000, 1_000_000_000);
			}
			while (Users.Any(u => u.Id == id));
			return id;
		}

		internal async Task<bool> MigrateAsync(ILogger logger, CancellationToken cancellationToken)
		{
			return await Database.ApplyMigrationsAsync(options =>
			{
				options.Logger = logger;
				options.MigrationsTableName = "__migrations";

				options.SourceAssembly = Assembly.GetExecutingAssembly();
				options.Path = "VerneMQ.Control.Database.SqlScripts";
			}, cancellationToken);
		}
	}
}
