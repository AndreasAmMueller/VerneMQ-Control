using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unclassified.TxLib;
using VerneMQ.Control.Database;
using VerneMQ.Control.Database.Entities;
using VerneMQ.Control.Security;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the authentication logic for the "backend".
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
	public class AuthController : Controller
	{
		private readonly ServerDbContext dbContext;
		private readonly IConfiguration configuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthController"/> class.
		/// </summary>
		/// <param name="dbContext">The database context.</param>
		/// <param name="configuration">The configuration.</param>
		public AuthController(ServerDbContext dbContext, IConfiguration configuration)
		{
			this.dbContext = dbContext;
			this.configuration = configuration;
		}

		/// <summary>
		/// MQTT authentication (VerneMQ WebHooks).
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> Mqtt()
		{
			int cacheSeconds = configuration.GetValue("Hosting:CacheTime", 60);
			cacheSeconds = Math.Max(cacheSeconds, 0);   // prevent negative values.
			Response.Headers.Add("cache-control", $"max-age={cacheSeconds}");

			string json = null;
			using (var sr = new StreamReader(Request.Body))
			{
				json = await sr.ReadToEndAsync();
			}

			string hook = Request.Headers["vernemq-hook"].ToString();
			var req = json.DeserializeJson<JObject>();

			string username = req.Value<string>("username")?.Trim().ToLower();
			string password = req.Value<string>("password")?.Trim();

			var user = dbContext.MqttUsers
				.Include(u => u.Permissions)
				.Where(u => u.Username == username)
				.FirstOrDefault();
			if (user == null)
				return Json(new { result = new { error = $"The user '{username}' is unknown" } });

			switch (hook)
			{
				case "auth_on_register":
					{
						if (!PasswordHelper.VerifyPassword(password, user.PasswordHash, out _))
							return Json(new { result = new { error = $"The password for user '{username}' is invalid" } });

						string clientId = req.Value<string>("client_id").Trim();
						if (string.IsNullOrWhiteSpace(user.ClientRegex) || Regex.Matches(clientId, user.ClientRegex).Count == 1)
							return Json(new { result = "ok" });

						return Json(new { result = new { error = $"The client id '{clientId}' for user '{username}' is not allowed" } });
					}
				case "auth_on_subscribe":
					{
						var topics = new JArray();
						var result = new JObject
						{
							["result"] = "ok",
							["topics"] = topics
						};

						foreach (var topicReq in req["topics"])
						{
							string topic = topicReq.Value<string>("topic");
							if (user.DoRewrite && !string.IsNullOrWhiteSpace(user.BaseTopic) && !topic.StartsWith(user.BaseTopic))
								topic = $"{user.BaseTopic.TrimEnd('/')}/{topic.TrimStart('/')}";

							bool isAllowed = user.Permissions
								.Where(p => p.CanRead)
								.OrderByDescending(p => p.Topic.Length)
								.Where(p =>
								{
									string source = p.Topic;
									if (user.DoRewrite && !string.IsNullOrWhiteSpace(user.BaseTopic))
										source = $"{user.BaseTopic.TrimEnd('/')}/{source.TrimStart('/')}";

									return MqttPermission.IsTopicMatch(source, topic);
								})
								.Any();

							topics.Add(new JObject
							{
								["topic"] = topic,
								["qos"] = isAllowed ? topicReq.Value<int>("qos") : 128
							});
						}

						return Json(result);
					}
				case "auth_on_publish":
					{
						string topic = req.Value<string>("topic");
						if (user.DoRewrite && !string.IsNullOrWhiteSpace(user.BaseTopic) && !topic.StartsWith(user.BaseTopic))
							topic = $"{user.BaseTopic.TrimEnd('/')}/{topic.TrimStart('/')}";

						bool isAllowed = user.Permissions
							.Where(p => p.CanWrite)
							.OrderByDescending(p => p.Topic.Length)
							.Where(p =>
							{
								string source = p.Topic;
								if (user.DoRewrite && !string.IsNullOrWhiteSpace(user.BaseTopic))
									source = $"{user.BaseTopic.TrimEnd('/')}/{source.TrimStart('/')}";

								return MqttPermission.IsTopicMatch(source, topic);
							})
							.Any();

						if (isAllowed)
							return Json(new { result = "ok", modifiers = new { topic } });

						return Json(new { result = new { error = $"No permission for '{username}' to publish to topic '{topic}'" } });
					}
				default:
					return BadRequest();
			}
		}

		/// <summary>
		/// Basic authentication for NgniX.
		/// </summary>
		/// <returns></returns>
		public IActionResult Basic()
		{
			var authUser = HttpContext.GetAuthUser(dbContext);
			if (authUser?.IsAdmin == true)
				return Ok();

			Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{Tx.T("Auth.Basic.Realm")}\"");
			return Unauthorized(Tx.T("Auth.Basic.Text"));
		}
	}
}
