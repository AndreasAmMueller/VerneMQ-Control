using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Unclassified.TxLib;
using VerneMQ.Control.Database;
using VerneMQ.Control.Database.Entities;
using VerneMQ.Control.Security;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the logic to maintain the users.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
	[Authorize]
	public class UsersController : Controller
	{
		private readonly ILogger logger;
		private readonly ServerDbContext dbContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="UsersController"/> class.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="dbContext">The database context.</param>
		public UsersController(ILogger<UsersController> logger, ServerDbContext dbContext)
		{
			this.logger = logger;
			this.dbContext = dbContext;
		}

		/// <summary>
		/// Lists all (web) users.
		/// </summary>
		/// <returns></returns>
		public IActionResult Index()
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();
			if (!authUser.IsAdmin)
				return Forbid();

			var list = dbContext.Users
				.OrderByDescending(u => u.IsEnabled)
				.ThenBy(u => u.Username)
				.ToList();

			ViewData["Title"] = Tx.T("Users.Index.Title");
			return View(list);
		}

		/// <summary>
		/// Create a new user.
		/// </summary>
		/// <returns></returns>
		public IActionResult Create()
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();
			if (!authUser.IsAdmin)
				return Forbid();

			ViewData["Title"] = Tx.T("Users.Index.Title");
			ViewData["ViewTitle"] = Tx.T("Users.Create.ViewTitle");
			return View(nameof(Edit), new WebUser
			{
				IsEnabled = true
			});
		}

		/// <summary>
		/// Edit a user.
		/// </summary>
		/// <param name="id">The user id.</param>
		/// <returns></returns>
		public IActionResult Edit(int id)
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();
			if (!authUser.IsAdmin)
				return Forbid();

			var user = dbContext.Users
				.Where(u => u.Id == id)
				.FirstOrDefault();
			if (user == null)
				return NotFound();

			ViewData["Title"] = Tx.T("Users.Index.Title");
			ViewData["ViewTitle"] = Tx.T("Users.Edit.ViewTitle");
			return View(user);
		}

		/// <summary>
		/// Saves changes to the user.
		/// </summary>
		/// <param name="model">The modified data.</param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(WebUser model)
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();
			if (!authUser.IsAdmin)
				return Forbid();

			ViewData["Title"] = Tx.T("Users.Index.Title");
			ViewData["ViewTitle"] = model.Id > 0 ? Tx.T("Users.Edit.ViewTitle") : Tx.T("Users.Create.ViewTitle");

			if (string.IsNullOrWhiteSpace(model.Username))
			{
				ModelState.AddModelError(nameof(model.Username), Tx.T("Users.Edit.Username.Empty"));
				return View(model);
			}

			model.Username = model.Username.Trim().ToLower();
			if (dbContext.Users
				.Where(u => u.Id != model.Id)
				.Where(u => u.Username == model.Username)
				.Any())
			{
				ModelState.AddModelError(nameof(model.Username), Tx.T("Users.Edit.Username.Duplicate"));
				return View(model);
			}

			var user = dbContext.Users
				.Where(u => u.Id == model.Id)
				.FirstOrDefault();
			if (user == null)
			{
				user = new WebUser
				{
					Id = dbContext.GetUserId()
				};
				dbContext.Users.Add(user);
			}

			user.IsAdmin = model.IsAdmin;
			user.IsEnabled = model.IsEnabled;
			user.Username = model.Username;

			if (!string.IsNullOrWhiteSpace(model.Password))
				user.PasswordHash = PasswordHelper.HashPassword(model.Password.Trim());

			if (user.Id == authUser.Id)
			{
				user.IsEnabled = true;
				user.IsAdmin = true;
			}

			try
			{
				dbContext.SaveChanges();
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"Saving (web)user failed: {ex.GetMessage()}");
				return StatusCode(StatusCodes.Status500InternalServerError);
			}
		}

		/// <summary>
		/// Deletes a user..
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
			if (!authUser.IsAdmin)
				return Forbid();

			if (authUser.Id == id)
				return BadRequest();

			var user = dbContext.Users
				.Where(u => u.Id == id)
				.FirstOrDefault();
			if (user == null)
				return NotFound();

			try
			{
				dbContext.Users.Remove(user);
				dbContext.SaveChanges();

				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"Deleting (web)user failed: {ex.GetMessage()}");
				return StatusCode(StatusCodes.Status500InternalServerError);
			}
		}
	}
}
