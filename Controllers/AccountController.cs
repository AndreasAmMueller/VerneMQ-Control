using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Unclassified.TxLib;
using VerneMQ.Control.Database;
using VerneMQ.Control.Database.Entities;
using VerneMQ.Control.Models;
using VerneMQ.Control.Security;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the logic for the account.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
	[Authorize]
	public class AccountController : Controller
	{
		private readonly ILogger logger;
		private readonly ServerDbContext dbContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="AccountController"/> class.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="dbContext">The database context.</param>
		public AccountController(ILogger<AccountController> logger, ServerDbContext dbContext)
		{
			this.logger = logger;
			this.dbContext = dbContext;
		}

		/// <summary>
		/// Shows the user's profile.
		/// </summary>
		/// <returns></returns>
		public IActionResult Index()
		{
			var authUser = HttpContext.GetAuthUser(dbContext);
			if (authUser == null)
				return Unauthorized();

			ViewData["Title"] = Tx.T("Account.Index.Title");
			return View(authUser);
		}

		/// <summary>
		/// Saves changes to the user.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Index(WebUser model)
		{
			var authUser = HttpContext.GetAuthUser(dbContext);
			if (authUser == null)
				return Unauthorized();

			if (string.IsNullOrWhiteSpace(model.Username))
			{
				ModelState.AddModelError(nameof(model.Username), Tx.T("Account.Index.Username.Empty"));
				return View(model);
			}

			model.Username = model.Username.Trim().ToLower();
			model.Password = model.Password?.Trim();
			if (dbContext.Users
				.Where(u => u.Id != authUser.Id)
				.Where(u => u.Username == model.Username)
				.Any())
			{
				ModelState.AddModelError(nameof(model.Username), Tx.T("Account.Index.Username.Duplicate"));
				return View(model);
			}

			try
			{
				authUser.Username = model.Username;

				if (!string.IsNullOrWhiteSpace(model.Password))
					authUser.PasswordHash = PasswordHelper.HashPassword(model.Password);

				dbContext.SaveChanges();

				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"Saving the user's profile failed: {ex.GetMessage()}");
				return StatusCode(StatusCodes.Status500InternalServerError);
			}
		}

		/// <summary>
		/// Shows the sign-in site.
		/// </summary>
		/// <returns></returns>
		[AllowAnonymous]
		public IActionResult Login()
		{
			ViewData["Title"] = Tx.T("Account.Login.Title");
			return View(new AccountViewModel());
		}

		/// <summary>
		/// Performs a sign-in attempt.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <param name="returnUrl">The return URL.</param>
		/// <returns></returns>
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(AccountViewModel model, string returnUrl)
		{
			ViewData["Title"] = Tx.T("Account.Login.Title");

			if (string.IsNullOrWhiteSpace(model.Username))
				ModelState.AddModelError(nameof(model.Username), Tx.T("Account.Login.Username.Empty"));

			if (string.IsNullOrWhiteSpace(model.Password))
				ModelState.AddModelError(nameof(model.Password), Tx.T("Account.Login.Password.Empty"));

			if (!ModelState.IsValid)
				return View(model);

			model.Username = model.Username.Trim().ToLower();
			model.Password = model.Password.Trim();

			var user = dbContext.Users
				.Where(u => u.IsEnabled)
				.Where(u => u.Username == model.Username)
				.FirstOrDefault();
			if (user == null)
			{
				ModelState.AddModelError(nameof(model.Username), Tx.T("Account.Login.Username.NotFound"));
				return View(model);
			}

			if (!PasswordHelper.VerifyPassword(model.Password, user.PasswordHash, out bool rehash))
			{
				ModelState.AddModelError(nameof(model.Password), Tx.T("Account.Login.Password.Wrong"));
				return View(model);
			}

			if (rehash)
			{
				try
				{
					user.PasswordHash = PasswordHelper.HashPassword(model.Password);
					dbContext.SaveChanges();
				}
				catch
				{ }
			}

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user.GetPrincipal(), new AuthenticationProperties
			{
				AllowRefresh = true,
				IsPersistent = model.RememberMe
			});

			if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);

			return RedirectToAction(nameof(BrokerUserController.Index), "BrokerUser");
		}

		/// <summary>
		/// Performs a sign-out.
		/// </summary>
		/// <returns></returns>
		[AllowAnonymous]
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction(nameof(BrokerUserController.Index), "BrokerUser");
		}
	}
}
