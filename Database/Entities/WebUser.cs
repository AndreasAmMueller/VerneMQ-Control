using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VerneMQ.Control.Database.Entities
{
	/// <summary>
	/// Represents a (web)user.
	/// </summary>
	public class WebUser
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
		/// Gets or sets a value indicating whether the user is enabled.
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the user is administrator.
		/// </summary>
		public bool IsAdmin { get; set; }
	}
}
