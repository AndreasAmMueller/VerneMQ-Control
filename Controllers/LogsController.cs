using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerneMQ.Control.Database;
using VerneMQ.Control.Security;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the logic to show the logs.
	/// </summary>
	[Authorize]
	public class LogsController : Controller
	{
		private readonly LogDbContext dbContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="LogsController"/> class.
		/// </summary>
		/// <param name="dbContext">The database context.</param>
		public LogsController(LogDbContext dbContext)
		{
			this.dbContext = dbContext;
		}

		/// <summary>
		/// Returns the basic view to list the log entries.
		/// </summary>
		/// <returns></returns>
		public IActionResult Index()
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();
			if (!authUser.IsAdmin)
				return Forbid();

			ViewData["Title"] = "Anwendungsprotokoll";
			return View();
		}

		/// <summary>
		/// Returns a partial list of log entries.
		/// </summary>
		/// <param name="id">The starting index.</param>
		/// <returns></returns>
		public IActionResult Get(int id)
		{
			var authUser = HttpContext.GetAuthUser();
			if (authUser == null)
				return Unauthorized();
			if (!authUser.IsAdmin)
				return Forbid();

			int entriesPerRequest = 20;
			try
			{
				var entries = dbContext.LogEntries
					.Where(e => id == 0 || e.Id < id)
					.OrderByDescending(e => e.Id)
					.Take(entriesPerRequest)
					.ToList();

				return Json(entries);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, ex.GetMessage());
			}
		}
	}
}
