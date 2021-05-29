using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using AMWD.Common.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace VerneMQ.Control
{
	/// <summary>
	/// The application starting point.
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Gets the product name.
		/// </summary>
		public static string Product { get; private set; }

		/// <summary>
		/// Gets the application version.
		/// </summary>
		public static string Version { get; private set; }

		private static int Main(string[] args)
		{
			string dir = GetBaseDirectory();
			var config = new ConfigurationBuilder()
				.SetBasePath(dir)
				.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true)
				.AddIniFile("/etc/vernemq/control.ini", optional: true, reloadOnChange: true)
				.Build();
			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(config)
				.CreateLogger();

			AppDomain.CurrentDomain.UnhandledException += (s, a) =>
			{
				string terminating = a.IsTerminating ? " (terminating)" : "";
				if (a.ExceptionObject is Exception ex)
				{
					Log.Fatal(ex, $"Unhandled exception ({ex.GetType().Name}) in AppDomain{terminating}: {ex.GetMessage()}");
				}
				else
				{
					Log.Fatal($"Unhandled exception ({a.ExceptionObject.GetType().Name}) in AppDomain{terminating}: {a.ExceptionObject}");
				}
			};

			Product = Assembly.GetExecutingAssembly()
				.GetCustomAttribute<AssemblyProductAttribute>()
				.Product;
			Version = Assembly.GetExecutingAssembly()
				.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
				.InformationalVersion;

			try
			{
				Console.WriteLine($"{Product} v{Version} is starting...");

				var host = CreateHostBuilder(args).Build();
				try
				{
					host.Start();
					Console.WriteLine($"{Product} v{Version} started");

					host.WaitForShutdown();
					Console.WriteLine($"{Product} v{Version} is shut down");
				}
				finally
				{
					host.Dispose();
				}
				return 0;
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Unhandled exception in Main: {ex.GetRecursiveMessage()}");
				Console.WriteLine($"{Product} v{Version} is shut down uncleanly");
				return 1;
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		private static IHostBuilder CreateHostBuilder(string[] args)
		{
			string dir = GetBaseDirectory();
			return Host.CreateDefaultBuilder(args)
				.ConfigureServices(services =>
				{
					services.AddOptions<HostOptions>().Configure(options => options.ShutdownTimeout = TimeSpan.FromSeconds(60));
				})
				.ConfigureWebHostDefaults(builder =>
				{
					builder.UseContentRoot(dir);
					builder.ConfigureAppConfiguration((_, configuration) =>
					{
						configuration.SetBasePath(dir);
						configuration.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true);
						configuration.AddIniFile("/etc/vernemq/control.ini", optional: true, reloadOnChange: true);
						configuration.AddEnvironmentVariables();
						if (args?.Any() == true)
							configuration.AddCommandLine(args);
					});
					builder.UseSerilog((hostingContext, configuration) =>
					{
						configuration.ReadFrom.Configuration(hostingContext.Configuration);
					});
					builder.UseDefaultServiceProvider((_, options) =>
					{
#if DEBUG
						options.ValidateScopes = true;
#endif
					});
					builder.UseKestrel((hostingContext, options) =>
					{
						string url = hostingContext.Configuration.GetValue<string>("ASPNETCORE_URLS");
						if (string.IsNullOrWhiteSpace(url))
						{
							string address = hostingContext.Configuration.GetValue("Hosting:Address", "127.0.0.1");
							int port = hostingContext.Configuration.GetValue("Hosting:Port", 5000);
							var ipAddress = NetworkHelper.ResolveHost(address);
							if (ipAddress == null)
								ipAddress = IPAddress.Loopback;

							options.Listen(ipAddress, port);
							Log.Information($"Listening on {ipAddress}, port {port}");
						}

						options.AddServerHeader = false;
						options.ConfigureEndpointDefaults(opts =>
						{
							opts.Protocols = HttpProtocols.Http1AndHttp2;
						});
					});
					builder.UseStartup<Startup>();
				})
				.UseSystemd();
		}

		private static string GetBaseDirectory()
		{
			string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (assemblyDir.Contains("/debug", StringComparison.OrdinalIgnoreCase) || assemblyDir.Contains("/release", StringComparison.OrdinalIgnoreCase) ||
				assemblyDir.Contains("\\debug", StringComparison.OrdinalIgnoreCase) || assemblyDir.Contains("\\release", StringComparison.OrdinalIgnoreCase))
			{
				var root = new DirectoryInfo(assemblyDir);
				while (root.Parent != null)
				{
					if (root.GetFiles().Where(fi => fi.Name.EndsWith(".csproj")).Any())
						return Directory.GetCurrentDirectory();

					root = root.Parent;
				}
			}
			return assemblyDir;
		}
	}
}
