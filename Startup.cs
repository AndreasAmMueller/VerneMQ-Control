using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Unclassified.TxLib;
using VerneMQ.Control.Database;
using VerneMQ.Control.Hubs;
using VerneMQ.Control.Security;
using VerneMQ.Control.Services;
using static System.Net.Mime.MediaTypeNames;

namespace VerneMQ.Control
{
	internal class Startup
	{
		internal static string WebRootPath { get; private set; }

		internal static string PersistentDataDirectory { get; private set; }

		private readonly IConfiguration configuration;

		public Startup(IWebHostEnvironment env, IConfiguration configuration)
		{
			string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			this.configuration = configuration;
			WebRootPath = env.WebRootPath;

			PersistentDataDirectory = configuration.GetValue<string>("Hosting:PersistentData");
			if (string.IsNullOrWhiteSpace(PersistentDataDirectory))
				PersistentDataDirectory = dir;

			if (!Path.IsPathRooted(PersistentDataDirectory))
				PersistentDataDirectory = Path.Combine(dir, PersistentDataDirectory);

			if (!Directory.Exists(PersistentDataDirectory))
				Directory.CreateDirectory(PersistentDataDirectory);
		}

		public void ConfigureServices(IServiceCollection services)
		{
			#region DB Connection

			services.AddDbContext<ServerDbContext>(options =>
			{
#if DEBUG
				options.EnableSensitiveDataLogging();
#endif
				options.UseDatabaseProvider(configuration.GetSection("Database"), opts =>
				{
					opts.AbsoluteBasePath = PersistentDataDirectory;
				});
			});

			if (!string.IsNullOrWhiteSpace(LogDbContext.GetPath(configuration)))
			{
				services.AddDbContext<LogDbContext>(options =>
				{
					options.UseSqlite(LogDbContext.GetConnectionString(configuration));
				});
			}

			#endregion DB Connection

			#region Configurations

			services.AddResponseCompression(options =>
			{
				// https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-3.1#compression-with-secure-protocol
				options.EnableForHttps = true;
				options.MimeTypes = ResponseCompressionDefaults.MimeTypes
					.Concat(new[]
					{
						"image/svg",
						"image/svg+xml",
						"image/x-icon",
						"application/manifest+json"
					});
			});
			services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

			#endregion Configurations

			#region Cookies

			services.Configure<AntiforgeryOptions>(options =>
			{
				options.Cookie.Name = "VerneMQ.Control.Security";
				options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
				options.Cookie.SameSite = SameSiteMode.Strict;
				options.FormFieldName = "__SecurityToken";
			});

			#endregion Cookies

			#region Services and Dependency Injection

			services.AddSingleton(configuration);
			services.AddSingletonHostedService<InitService>();
			services.AddSingletonHostedService<BackgroundUpdateService>();

			#endregion Services and Dependency Injection

			#region Authentication

			string keyDir = Path.Combine(PersistentDataDirectory, "keys");
			if (!Directory.Exists(keyDir))
				Directory.CreateDirectory(keyDir);

			services.AddDataProtection()
				.PersistKeysToFileSystem(new DirectoryInfo(keyDir));

			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(options =>
				{
					options.Cookie.Name = "VerneMQ.Control.Auth";
					options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
					options.Cookie.SameSite = SameSiteMode.Strict;

					options.LoginPath = "/account/login";
					options.LogoutPath = "/account/logout";
					options.AccessDeniedPath = "/error/403";
					options.EventsType = typeof(CustomCookieAuthenticationEvents);
					options.SlidingExpiration = true;
					options.ExpireTimeSpan = TimeSpan.FromDays(90);
				});
			services.AddScoped<CustomCookieAuthenticationEvents>();

			#endregion Authentication

			#region Runtime

			services.AddSignalR();
			var mvc = services.AddControllersWithViews()
				.AddNewtonsoftJson(options =>
				{
					options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
				});
#if DEBUG
			mvc.AddRazorRuntimeCompilation();
#endif

			#endregion Runtime
		}

		public void Configure(IApplicationBuilder app, InitService init)
		{
			bool isDev = configuration.GetValue("ASPNETCORE_ENVIRONMENT", "Production") == "Development";

			app.UseTx();
			app.UseProxyHosting();
			app.UseResponseCompression();

			app.Use(async (httpContext, next) =>
			{
				string path = httpContext.Request.Path.ToString();
				if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using var httpClient = new HttpClient();
						
						string url = configuration.GetValue("VerneMQ:Health", "http://localhost:8888/health");
						var urlMatch = Regex.Match(url, @"^(https?:\/\/)(.*)@(.*)$");
						if (urlMatch.Success)
						{
							httpClient.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(urlMatch.Groups[2].Value)));
							url = $"{urlMatch.Groups[1].Value}{urlMatch.Groups[3].Value}";
						}
						
						var res = await httpClient.GetAsync(url);
						httpContext.Response.StatusCode = init.IsSuccess ? (int)res.StatusCode : 503;

						string json = await res.Content.ReadAsStringAsync();
						var status = json.DeserializeJson<JObject>();

						status["control"] = init.IsSuccess ? "OK" : "DOWN";
						httpContext.Response.ContentType = Application.Json;
						await httpContext.Response.WriteAsync(status.SerializeJson());
					}
					catch (Exception ex)
					{
						httpContext.Response.StatusCode = 500;
						httpContext.Response.ContentType = Text.Plain;
						await httpContext.Response.WriteAsync(ex.GetRecursiveMessage());
					}
					return;
				}

				if (!path.StartsWith("/error", StringComparison.OrdinalIgnoreCase))
					httpContext.Items["OriginalPath"] = path;

				if (!init.IsSuccess)
				{
					httpContext.Response.Clear();
					httpContext.Response.StatusCode = 500;
					httpContext.Response.ContentType = "text/plain; charset=utf-8";
					await httpContext.Response.WriteAsync(Tx.T("Startup.InitError", new Dictionary<string, string>
					{
						{ "version", Program.Version },
						{ "time", DateTime.Now.ToString($"{Tx.T("Tx:date.year month day.tab")} {Tx.T("Tx:time.hour minute second.tab")}") }
					}));
					return;
				}

				await next();
			});

			if (isDev)
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/error");
			}
			app.UseStatusCodePagesWithReExecute("/error/{0}");

#if DEBUG
			app.UseStaticFiles();
			var _ = CultureInfo.InvariantCulture;
#else
			app.UseStaticFiles(new StaticFileOptions
			{
				OnPrepareResponse = context =>
				{
					string extension = Path.GetExtension(context.File.Name);
					switch (extension)
					{
						case "js":
						case "css":
						case "eot":
						case "svg":
						case "ttf":
						case "woff":
						case "woff2":
							context.Context.Response.Headers["Cache-Control"] = $"public,max-age={(int)TimeSpan.FromDays(7).TotalSeconds}";
							context.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddDays(28).ToString("R", CultureInfo.InvariantCulture);
							break;

						case "gif":
						case "jpg":
						case "png":
						case "webp":
							context.Context.Response.Headers["Cache-Control"] = $"public,max-age={(int)TimeSpan.FromDays(28).TotalSeconds}";
							context.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddMonths(3).ToString("R", CultureInfo.InvariantCulture);
							break;
					}
				}
			});
#endif

			app.UseAuthentication();
			app.UseRouting();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapHub<WebHub>("/live");

				endpoints.MapControllerRoute(
					name: "defaultNoAction",
					pattern: "{controller=MqttUser}/{id:int}",
					defaults: new { action = "Index" });
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=MqttUser}/{action=Index}/{id?}");
			});
		}
	}
}
