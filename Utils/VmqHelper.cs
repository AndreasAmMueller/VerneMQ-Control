using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VerneMQ.Control.Models;

namespace VerneMQ.Control.Utils
{
	internal static class VmqHelper
	{
		public static async Task<Dictionary<string, ulong>> GetMetrics(string url, ILogger logger = null, CancellationToken cancellationToken = default)
		{
			try
			{
				using var httpClient = new HttpClient();
				var urlMatch = Regex.Match(url, @"^(https?:\/\/)(.*)@(.*)$");
				if (urlMatch.Success)
				{
					httpClient.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(urlMatch.Groups[2].Value)));
					url = $"{urlMatch.Groups[1].Value}{urlMatch.Groups[3].Value}";
				}

				var res = await httpClient.GetAsync(url, cancellationToken);
				if (!res.IsSuccessStatusCode)
					return null;

				string content = await res.Content.ReadAsStringAsync(cancellationToken);
				var matches = Regex.Matches(content, @"^([^#].+){.*} (.+)$", RegexOptions.Multiline);
				var metrics = new Dictionary<string, ulong>();
				foreach (Match match in matches)
				{
					if (!metrics.ContainsKey(match.Groups[1].Value))
						metrics.Add(match.Groups[1].Value, 0);

					metrics[match.Groups[1].Value] += ulong.Parse(match.Groups[2].Value);
				}

				return metrics;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, $"Loading metrics failed: {ex.GetMessage()}");
				return null;
			}
		}

		public static async Task<List<VmqClient>> GetClients(string vmqAdminPath, ILogger logger = null, CancellationToken cancellationToken = default)
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					WorkingDirectory = Path.GetDirectoryName(vmqAdminPath),
					FileName = Path.GetFileName(vmqAdminPath),
					Arguments = "session show --client_id --statename --clean_session --user --peer_host --peer_port --protocol --session_started_at",
					CreateNoWindow = true,
					RedirectStandardOutput = true
				};
				var process = Process.Start(psi);
				
				var lines = new List<string>();
				while ((!process.HasExited && !process.StandardOutput.EndOfStream) && !cancellationToken.IsCancellationRequested)
				{
					string line = await process.StandardOutput.ReadLineAsync();
					if (line != null)
						lines.Add(line);
				}
				
				if (cancellationToken.IsCancellationRequested && !process.HasExited)
					process.Kill();

				if (!lines.Any())
					return new();

				var clients = new List<VmqClient>();
				foreach (string line in lines.Where(l => l.StartsWith("|")).Skip(1))
				{
					string l = line.Trim('|');
					string[] parts = l.Split('|');

					var client = new VmqClient
					{
						CleanSession = bool.Parse(parts[0].Trim()),
						ClientId = parts[1].Trim(),
						IpAddress = parts[2].Trim(),
						Port = int.Parse(parts[3].Trim()),
						Protocol = int.Parse(parts[4].Trim()),
						SessionStart = ulong.Parse(parts[5].Trim()),
						Status = parts[6].Trim(),
						UserName = parts[7].Trim()
					};
					clients.Add(client);
				}

				return clients;
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, $"Loading clients failed: {ex.GetMessage()}");
				return new();
			}
		}
	}
}
