using System.Data.SQLite;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VerneMQ.Control.Database.Entities;

namespace VerneMQ.Control.Database
{
	/// <summary>
	/// The database context for the <see cref="Serilog"/> log entries.
	/// </summary>
	/// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
	public class LogDbContext : DbContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LogDbContext"/> class.
		/// </summary>
		/// <param name="options">The options.</param>
		public LogDbContext(DbContextOptions<LogDbContext> options)
			: base(options)
		{ }

		/// <summary>
		/// Gets (or sets) the log entries.
		/// </summary>
		public DbSet<LogEntry> LogEntries { get; protected set; }

		/// <summary>
		/// Gets the path to the log database.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static string GetPath(IConfiguration configuration)
		{
			string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var logSection = configuration.GetSection("Serilog");
			if (logSection.Exists())
			{
				var writeTo = logSection.GetSection("WriteTo");
				if (writeTo.Exists())
				{
					foreach (var section in writeTo.GetChildren())
					{
						if (section.GetValue<string>("Name") == "SQLite")
						{
							string path = section.GetValue<string>("Args:sqliteDbPath");
							if (!string.IsNullOrWhiteSpace(path))
							{
								if (Path.IsPathRooted(path))
									return path;

								return Path.Combine(dir, path);
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the connection string.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static string GetConnectionString(IConfiguration configuration)
		{
			var builder = new SQLiteConnectionStringBuilder
			{
				ForeignKeys = true,
				DataSource = GetPath(configuration)
			};
			return builder.ConnectionString;
		}
	}
}
