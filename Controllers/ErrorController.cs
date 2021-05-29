using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
			ViewData["Title"] = $"Fehler {id}";

			string requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
			string originalPath = HttpContext.Items["OriginalPath"]?.ToString()?.Trim();
			if (!string.IsNullOrWhiteSpace(originalPath))
				originalPath = $" (<code>{originalPath}</code>)";

			var model = new ErrorViewModel
			{
				ErrorCode = id,
				Title = $"Fehler {id}",
				Description = $"Es ist ein Fehler aufgetreten. Sollte dies häufiger passieren, kontaktieren Sie einen Administrator. Anfrage-ID: <code>{requestId}</code>.",
				Icon = "fas fa-exclamation-triangle"
			};

			switch (id)
			{
				case 400:
					model.Title = "Ungültige Anfrage";
					model.Description = "Sie haben eine ungültige oder unvollständige Anfrage gesendet, die nicht verarbeitet werden kann.";
					break;

				case 401:
					model.Icon = "fas fa-hand-paper";
					model.Title = "Authorizierung erforderlich";
					model.Description = "Es konnte nicht geprüft werden, ob Sie zum Zugriff auf das Dokument berechtigt sind. Bitte melden Sie sich an.";
					break;

				case 403:
					model.Icon = "fas fa-ban";
					model.Title = "Verboten";
					model.Description = "Sie verfügen nicht über die nötigen Berechtigungen, um auf das Dokument zuzugreifen.";
					break;

				case 404:
					model.Icon = "fas fa-search";
					model.Title = "Nicht gefunden";
					model.Description = $"Das angeforderte Dokument{originalPath} wurde nicht gefunden.";
					break;

				case 405:
					model.Icon = "fas fa-plug";
					model.Title = "Methode nicht erlaubt";
					model.Description = $"Das Dokument{originalPath} wurde mit der falschen Methode angefragt (z.B. GET statt POST).";
					break;

				case 410:
					model.Icon = "fas fa-shoe-prints";
					model.Title = "Vergangen";
					model.Description = "Das angefragte Dokument ist nicht mehr verfügbar.";
					break;

				case 500:
					model.Icon = "fas fa-server";
					model.Title = "Interner Serverfehler";
					model.Description = "Der Server hat einen internen Fehler oder eine fehlerhafte Konfiguration festgestellt.";
					break;
			}

			return View(model);
		}
	}
}
