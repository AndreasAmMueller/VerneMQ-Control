using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VerneMQ.Control.Database.Entities
{
	/// <summary>
	/// A <see cref="Serilog"/> entry.
	/// </summary>
	[Table("Logs")]
	public class LogEntry
	{
		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		[Key]
		public long Id { get; set; }

		/// <summary>
		/// Gets or sets the timestamp in the database.
		/// </summary>
		[JsonIgnore]
		[Column("Timestamp")]
		public string DbTimestamp { get; set; }

		/// <summary>
		/// Gets or sets the log level.
		/// </summary>
		[MaxLength(10)]
		public string Level { get; set; }

		/// <summary>
		/// Gets or sets the exception.
		/// </summary>
		public string Exception { get; set; }

		/// <summary>
		/// Gets or sets raw log message.
		/// </summary>
		[JsonIgnore]
		[Column("RenderedMessage")]
		public string RawMessage { get; set; }

		/// <summary>
		/// Gets or sets the properties as json.
		/// </summary>
		[JsonIgnore]
		[Column("Properties")]
		public string PropertiesJson { get; set; }

		/// <summary>
		/// Gets the timestamp.
		/// </summary>
		[NotMapped]
		public DateTime Timestamp => DateTime.Parse(DbTimestamp).AsUtc();

		/// <summary>
		/// Gets the rendered message.
		/// </summary>
		[NotMapped]
		public string Message => ToString();

		/// <summary>
		/// Gets the properties.
		/// </summary>
		[NotMapped]
		public JObject Properties => PropertiesJson?.DeserializeJson<JObject>();

		/// <inheritdoc/>
		public override string ToString()
		{
			string output = RawMessage;

			string pattern = @"\{([0-9a-zA-Z.:]+)\}";
			var matches = Regex.Matches(RawMessage, pattern);

			foreach (Match match in matches)
			{
				if (match.Success)
				{
					string key = match.Value.Replace("{", "").Replace("}", "");
					string format = null;
					if (key.Contains(":"))
					{
						string[] split = key.Split(':');
						key = split[0];
						format = split[1];
					}

					if (Properties.TryGetValue(key, out var token))
					{
						string replace = null;
						replace = token.Type switch
						{
							JTokenType.Float => string.IsNullOrWhiteSpace(format)
								? token.Value<decimal>().ToString()
								: token.Value<decimal>().ToString(format),
							JTokenType.Integer => string.IsNullOrWhiteSpace(format)
								? token.Value<long>().ToString()
								: token.Value<long>().ToString(format),
							_ => token.ToString(),
						};
						output = output.Replace(match.Value, replace);
					}
				}
			}

			return output.Trim();
		}
	}
}
