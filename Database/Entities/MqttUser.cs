using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerneMQ.Control.Database.Entities
{
	/// <summary>
	/// Represents a MQTT user.
	/// </summary>
	public class MqttUser
	{
		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		[Key]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the username.
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// Gets or sets the password.
		/// </summary>
		[NotMapped]
		public string Password { get; set; }

		/// <summary>
		/// Gets or sets the password hash.
		/// </summary>
		public string PasswordHash { get; set; }

		/// <summary>
		/// Gets or sets the client regex.
		/// </summary>
		public string ClientRegex { get; set; } = "^[0-9a-zA-Z-_]+$";

		/// <summary>
		/// Gets or sets the base topic.
		/// </summary>
		public string BaseTopic { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether not matching topics should be rewritten.
		/// </summary>
		public bool DoRewrite { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the user is enabled.
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Gets or sets the permissions json-serialized.
		/// </summary>
		[NotMapped]
		public string PermissionsJson { get; set; }

		/// <summary>
		/// Gets or sets the permissions.
		/// </summary>
		public virtual List<MqttPermission> Permissions { get; set; } = new();
	}
}
