using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unclassified.TxLib;
using VerneMQ.Control.Database;
using VerneMQ.Control.Database.Entities;
using VerneMQ.Control.Security;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the logic for MQTT users.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
	[Authorize]
	public class MqttUserController : Controller
	{
		private readonly ILogger logger;
		private readonly ServerDbContext dbContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="MqttUserController"/> class.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="dbContext">The database context.</param>
		public MqttUserController(ILogger<MqttUserController> logger, ServerDbContext dbContext)
		{
			this.logger = logger;
			this.dbContext = dbContext;
		}

		/// <summary>
		/// Lists the MQTT users.
		/// </summary>
		/// <returns></returns>
		public IActionResult Index()
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();

			var list = dbContext.MqttUsers
				.OrderByDescending(u => u.IsEnabled)
				.ThenBy(u => u.Username)
				.ToList();

			ViewData["Title"] = Tx.T("MqttUser.Index.Title");
			return View(list);
		}

		/// <summary>
		/// Create a new MQTT user.
		/// </summary>
		/// <returns></returns>
		public IActionResult Create()
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();

			ViewData["Title"] = Tx.T("MqttUser.Index.Title");
			ViewData["ViewTitle"] = Tx.T("MqttUser.Create.ViewTitle");
			return View(nameof(Edit), new MqttUser
			{
				IsEnabled = true,
				PermissionsJson = "[]"
			});
		}

		/// <summary>
		/// Edit a MQTT user.
		/// </summary>
		/// <param name="id">The user id.</param>
		/// <returns></returns>
		public IActionResult Edit(int id)
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();

			var user = dbContext.MqttUsers
				.Include(u => u.Permissions)
				.Where(u => u.Id == id)
				.FirstOrDefault();
			if (user == null)
				return NotFound();

			user.Permissions.ForEach(p => p.User = null);
			user.PermissionsJson = user.Permissions.SerializeJson();

			ViewData["Title"] = Tx.T("MqttUser.Index.Title");
			ViewData["ViewTitle"] = Tx.T("MqttUser.Edit.ViewTitle");

			return View(user);
		}

		/// <summary>
		/// Saves the changes to the MQTT user.
		/// </summary>
		/// <param name="model">The modified data.</param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(MqttUser model)
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();

			model.Permissions = model.PermissionsJson.DeserializeJson<List<MqttPermission>>();

			ViewData["Title"] = Tx.T("MqttUser.Index.Title");
			ViewData["ViewTitle"] = model.Id > 0 ? Tx.T("MqttUser.Edit.ViewTitle") : Tx.T("MqttUser.Create.ViewTitle");

			if (string.IsNullOrWhiteSpace(model.Username))
			{
				ModelState.AddModelError(nameof(model.Username), Tx.T("MqttUser.Edit.Username.Empty"));
				return View(model);
			}

			model.Username = model.Username.Trim().ToLower();
			if (dbContext.MqttUsers
				.Where(u => u.Id != model.Id)
				.Where(u => u.Username == model.Username)
				.Any())
			{
				ModelState.AddModelError(nameof(model.Username), Tx.T("MqttUser.Edit.Username.Duplicate"));
				return View(model);
			}

			model.Password = model.Password?.Trim();
			model.ClientRegex = model.ClientRegex?.Trim();

			var user = dbContext.MqttUsers
				.Include(u => u.Permissions)
				.Where(u => u.Id == model.Id)
				.FirstOrDefault();
			if (user == null)
			{
				user = new MqttUser
				{
					Id = dbContext.GetMqttUserId()
				};
				dbContext.MqttUsers.Add(user);
			}

			user.Username = model.Username;
			user.IsEnabled = model.IsEnabled;
			user.ClientRegex = model.ClientRegex;
			user.BaseTopic = model.BaseTopic?.Trim();
			user.DoRewrite = model.DoRewrite;

			if (!string.IsNullOrWhiteSpace(model.Password))
				user.PasswordHash = PasswordHelper.HashPassword(model.Password);

			dbContext.MqttPermissions.RemoveRange(user.Permissions);
			foreach (var permission in model.Permissions)
			{
				if (string.IsNullOrWhiteSpace(permission.Topic))
					continue;

				dbContext.MqttPermissions.Add(new MqttPermission
				{
					User = user,
					Topic = permission.Topic.Trim(),
					CanRead = permission.CanRead,
					CanWrite = permission.CanWrite
				});
			}

			try
			{
				dbContext.SaveChanges();
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"Saving MQTT user failed: {ex.GetMessage()}");
				return StatusCode(StatusCodes.Status500InternalServerError);
			}
		}

		/// <summary>
		/// Deletes a MQTT user.
		/// </summary>
		/// <param name="id">The user id.</param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Delete(int id)
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();

			var user = dbContext.MqttUsers
				.Include(u => u.Permissions)
				.Where(u => u.Id == id)
				.FirstOrDefault();
			if (user == null)
				return NotFound();

			try
			{
				dbContext.MqttPermissions.RemoveRange(user.Permissions);
				dbContext.MqttUsers.Remove(user);
				dbContext.SaveChanges();
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"Deleting MQTT user failed: {ex.GetType()}");
				return StatusCode(StatusCodes.Status500InternalServerError);
			}
		}
	}
}
