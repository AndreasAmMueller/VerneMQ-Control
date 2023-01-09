using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
		private static DateTime lastMetricsError = DateTime.MinValue;

		private static DateTime lastClientsError = DateTime.MinValue;

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
				// action cancelled
				return new();
			}
			catch (Exception ex)
			{
				if (DateTime.UtcNow - lastMetricsError > TimeSpan.FromMinutes(5))
				{
					logger?.LogError(ex, $"Loading metrics failed: {ex.GetMessage()}");
					lastMetricsError = DateTime.UtcNow;
				}

				return new();
			}
		}

		public static async Task<List<VmqClient>> GetClients(string vmqAdminPath, ILogger logger = null, CancellationToken cancellationToken = default)
		{
			try
			{
				var clients = new List<VmqClient>();
				var lineRegex = new Regex(@"^\|(.*)\|(.*)\|([0-9a-fA-F.: ]+)\|([0-9 ]+)\|([0-9 ]+)\|([0-9 ]+)\|(.*)\|(.*)\|$");

				var psi = new ProcessStartInfo
				{
					WorkingDirectory = Path.GetDirectoryName(vmqAdminPath),
					FileName = Path.GetFileName(vmqAdminPath),
					Arguments = "session show --client_id --statename --clean_session --user --peer_host --peer_port --protocol --session_started_at",
					CreateNoWindow = true,
					RedirectStandardOutput = true
				};
				var process = Process.Start(psi);

				while (!process.HasExited && !process.StandardOutput.EndOfStream && !cancellationToken.IsCancellationRequested)
				{
					try
					{
						string line = await process.StandardOutput.ReadLineAsync();
						var regex = lineRegex.Match(line);
						if (regex.Success)
						{
							var client = new VmqClient
							{
								CleanSession = bool.Parse(regex.Groups[1].Value.Trim()),
								ClientId = regex.Groups[2].Value.Trim(),
								IpAddress = regex.Groups[3].Value.Trim(),
								Port = int.Parse(regex.Groups[4].Value.Trim()),
								Protocol = int.Parse(regex.Groups[5].Value.Trim()),
								SessionStart = ulong.Parse(regex.Groups[6].Value.Trim()),
								Status = regex.Groups[7].Value.Trim(),
								UserName = regex.Groups[8].Value.Trim()
							};

							clients.Add(client);
						}
					}
					catch
					{
						// keep it quiet - it's ok
					}
				}

				if (cancellationToken.IsCancellationRequested && !process.HasExited)
					process.Kill(entireProcessTree: true);

				return clients;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				// action cancelled
				return new();
			}
			catch (Exception ex)
			{
				if (DateTime.UtcNow - lastClientsError > TimeSpan.FromMinutes(5))
				{
					logger?.LogError(ex, $"Loading clients failed: {ex.GetMessage()}");
					lastClientsError = DateTime.UtcNow;
				}

				return new();
			}
		}
	}
}
