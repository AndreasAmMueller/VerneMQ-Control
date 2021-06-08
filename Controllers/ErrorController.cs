using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Unclassified.TxLib;
using VerneMQ.Control.Models;

namespace VerneMQ.Control.Controllers
{
	/// <summary>
	/// Implements the error handling.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
	[Route("Error")]
	public class ErrorController : Controller
	{
		/// <summary>
		/// Shows the default error page.
		/// </summary>
		/// <returns></returns>
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Index()
		{
			return Index(500);
		}

		/// <summary>
		/// Shows the specified error page.
		/// </summary>
		/// <param name="id">The error number (HTTP Status).</param>
		/// <returns></returns>
		[Route("{id}")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Index(int id)
		{
			if (id <= 0) id = 500;
			ViewData["Title"] = Tx.T("Error.Index.Title", new Dictionary<string, string> { { "id", id.ToString() } });

			string requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
			string originalPath = HttpContext.Items["OriginalPath"]?.ToString()?.Trim();
			if (!string.IsNullOrWhiteSpace(originalPath))
				originalPath = $" (<code>{originalPath}</code>)";

			var model = new ErrorViewModel
			{
				ErrorCode = id,
				Title = Tx.T("Error.Index.Title", new Dictionary<string, string> { { "id", id.ToString() } }),
				Description = Tx.T("Error.Index.Description", new Dictionary<string, string> { { nameof(requestId), requestId } }),
				Icon = "fas fa-exclamation-triangle"
			};

			switch (id)
			{
				case 400:
					model.Title = Tx.T($"Error.Index.{id}.Title");
					model.Description = Tx.T($"Error.Index.{id}.Description");
					break;

				case 401:
					model.Icon = "fas fa-hand-paper";
					model.Title = Tx.T($"Error.Index.{id}.Title");
					model.Description = Tx.T($"Error.Index.{id}.Description");
					break;

				case 403:
					model.Icon = "fas fa-ban";
					model.Title = Tx.T($"Error.Index.{id}.Title");
					model.Description = Tx.T($"Error.Index.{id}.Description");
					break;

				case 404:
					model.Icon = "fas fa-search";
					model.Title = Tx.T($"Error.Index.{id}.Title");
					model.Description = Tx.T($"Error.Index.{id}.Description", new Dictionary<string, string> { { nameof(originalPath), originalPath } });
					break;

				case 405:
					model.Icon = "fas fa-plug";
					model.Title = Tx.T($"Error.Index.{id}.Title");
					model.Description = Tx.T($"Error.Index.{id}.Description", new Dictionary<string, string> { { nameof(originalPath), originalPath } });
					break;

				case 410:
					model.Icon = "fas fa-shoe-prints";
					model.Title = Tx.T($"Error.Index.{id}.Title");
					model.Description = Tx.T($"Error.Index.{id}.Description");
					break;

				case 500:
					model.Icon = "fas fa-server";
					model.Title = Tx.T($"Error.Index.{id}.Title");
					model.Description = Tx.T($"Error.Index.{id}.Description");
					break;
			}

			return View(model);
		}
	}
}
