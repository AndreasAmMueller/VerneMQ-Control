using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using VerneMQ.Control.Database;
using VerneMQ.Control.Database.Entities;

namespace VerneMQ.Control.Security
{
	/// <summary>
	/// Extensions for better usabillity.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Gets the principal.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns></returns>
		public static ClaimsPrincipal GetPrincipal(this WebUser user)
		{
			var claims = new[]
			{
				new Claim(nameof(user.Id), user.Id.ToString()),
				new Claim(nameof(user.Username), user.Username),
				//new Claim(nameof(user.EmailAddress), user.EmailAddress ?? ""),
				//new Claim(nameof(user.Firstname), user.Firstname ?? ""),
				//new Claim(nameof(user.Lastname), user.Lastname ?? ""),
				new Claim(nameof(user.IsAdmin), user.IsAdmin.ToString())
			};
			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			return new ClaimsPrincipal(identity);
		}

		/// <summary>
		/// Gets the user.
		/// </summary>
		/// <param name="principal">The principal.</param>
		/// <param name="dbContext">The database context.</param>
		/// <returns></returns>
		public static WebUser GetUser(this ClaimsPrincipal principal, ServerDbContext dbContext = null)
		{
			if (!int.TryParse(principal.FindFirstValue(nameof(WebUser.Id)), out int id))
				return null;

			if (dbContext == null)
				return new WebUser
				{
					Id = id,
					Username = principal.FindFirstValue(nameof(WebUser.Username)),
					//EmailAddress = principal.FindFirstValue(nameof(WebUser.EmailAddress)),
					//Firstname = principal.FindFirstValue(nameof(WebUser.Firstname)),
					//Lastname = principal.FindFirstValue(nameof(WebUser.Lastname)),
					IsEnabled = true,
					IsAdmin = bool.Parse(principal.FindFirstValue(nameof(WebUser.IsAdmin)))
				};

			return dbContext.Users
				.Where(u => u.IsEnabled)
				.Where(u => u.Id == id)
				.FirstOrDefault();
		}

		/// <summary>
		/// Gets the authentication user.
		/// </summary>
		/// <param name="httpContext">The HTTP context.</param>
		/// <param name="dbContext">The database context.</param>
		/// <returns></returns>
		public static WebUser GetAuthUser(this HttpContext httpContext, ServerDbContext dbContext = null)
		{
			if (httpContext.User.Identity.IsAuthenticated)
				return httpContext.User.GetUser(dbContext);

			if (dbContext == null)
				return null;

			string authHeader = httpContext.Request.Headers["Authorization"].ToString();
			if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase))
				return null;

			authHeader = authHeader.Replace("basic", "", StringComparison.OrdinalIgnoreCase).Trim();
			string plain = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader));

			string username = plain.Split(':').First().Trim().ToLower();
			string password = plain.Split(':').Last().Trim();

			var user = dbContext.Users
				.Where(u => u.IsEnabled)
				.Where(u => u.Username == username)
				.FirstOrDefault();
			if (user == null)
				return null;

			if (!PasswordHelper.VerifyPassword(password, user.PasswordHash, out bool rehash))
				return null;

			if (rehash)
			{
				try
				{
					user.PasswordHash = PasswordHelper.HashPassword(password);
					dbContext.SaveChanges();
				}
				catch
				{ }
			}

			return user;
		}
	}
}
