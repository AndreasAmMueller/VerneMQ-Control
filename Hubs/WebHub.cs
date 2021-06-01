using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using VerneMQ.Control.Security;

namespace VerneMQ.Control.Hubs
{
	/// <summary>
	/// Represents the web hub for signalr clients.
	/// </summary>
	/// <seealso cref="Microsoft.AspNetCore.SignalR.Hub" />
	public class WebHub : Hub
	{
		internal const string Authenticated = "__authenticated_";

		/// <inheritdoc/>
		public override async Task OnConnectedAsync()
		{
			await base.OnConnectedAsync();

			if (!Context.User.Identity.IsAuthenticated)
				return;

			var authUser = Context.GetHttpContext().GetAuthUser();
			if (authUser == null)
				return;

			await Groups.AddToGroupAsync(Context.ConnectionId, Authenticated);
		}

		/// <inheritdoc/>
		public override async Task OnDisconnectedAsync(Exception exception)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, Authenticated);
			await base.OnDisconnectedAsync(exception);
		}
	}
}
