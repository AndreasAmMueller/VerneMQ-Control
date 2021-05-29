using System.ComponentModel.DataAnnotations.Schema;

namespace VerneMQ.Control.Database.Entities
{
	/// <summary>
	/// Represents a access permission.
	/// </summary>
	public class MqttPermission
	{
		/// <summary>
		/// Gets or sets the user id.
		/// </summary>
		[ForeignKey(nameof(User))]
		public int UserId { get; set; }

		/// <summary>
		/// Gets or sets the user.
		/// </summary>
		public virtual MqttUser User { get; set; }

		/// <summary>
		/// Gets or sets the topic.
		/// </summary>
		public string Topic { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether read access is granted.
		/// </summary>
		public bool CanRead { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether write access is granted.
		/// </summary>
		public bool CanWrite { get; set; }

		/// <summary>
		/// Determines whether a topic matches a definition.
		/// </summary>
		/// <param name="topic">The topic definition.</param>
		/// <param name="toCheck">The topic to check.</param>
		/// <returns></returns>
		public static bool IsTopicMatch(string topic, string toCheck)
		{
			if (string.IsNullOrWhiteSpace(toCheck))
				return false;

			string[] source = topic.Split('/');
			string[] check = toCheck.Split('/');

			if (check.Length < source.Length)
				return false;

			int i;
			for (i = 0; i < source.Length; i++)
			{
				if (source[i] == check[i])
					continue;

				if (source[i] == "+")
					continue;

				if (source[i] == "#")
					return true;

				break;
			}

			return i == source.Length && check.Length == source.Length;
		}
	}
}
