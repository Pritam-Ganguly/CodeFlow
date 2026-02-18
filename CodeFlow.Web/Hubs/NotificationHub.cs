using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CodeFlow.Web.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task AddConnectionToGroup()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(userId != null)
            {
                var groupName = $"User_Group_{userId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.Group(groupName).SendAsync("LogOutput", "Added successfully to group");
            }
        }

        public override async Task OnConnectedAsync()
        {
            await AddConnectionToGroup();
            await base.OnConnectedAsync();
        }
    }
}
