namespace VerneMQ.Control.Models
{
	/// <summary>
	/// Transfer class for sign in.
	/// </summary>
	public class AccountViewModel
	{
		/// <summary>
		/// Gets or sets the username.
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// Gets or sets the password.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to persist the session cookie.
		/// </summary>
		public bool RememberMe { get; set; }
	}
}
