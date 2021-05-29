using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VerneMQ.Control.Database;

namespace VerneMQ.Control.Security
{
	internal class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
	{
		private readonly ServerDbContext dbContext;

		public CustomCookieAuthenticationEvents(ServerDbContext dbContext)
		{
			this.dbContext = dbContext;
		}

		public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
		{
			var httpUser = context.Principal.GetUser();
			if (httpUser == null)
				return;

			var dbUser = context.Principal.GetUser(dbContext);
			if (dbUser == null)
			{
				context.RejectPrincipal();
				await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
				return;
			}

			if (httpUser.Username != dbUser.Username ||
				//httpUser.EmailAddress != dbUser.EmailAddress ||
				//httpUser.Firstname != dbUser.Firstname ||
				//httpUser.Lastname != dbUser.Lastname ||
				httpUser.IsAdmin != dbUser.IsAdmin)
			{
				context.ReplacePrincipal(dbUser.GetPrincipal());
				context.ShouldRenew = true;
			}
		}
	}
}
