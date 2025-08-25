using BlazorApp6.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BlazorApp6.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? "User Unknown";
            _chatService.AddUser(Context.ConnectionId, userName);

           
            var recentMessages = _chatService.GetRecentMessages();
            await Clients.Caller.SendAsync("LoadRecentMessages", recentMessages);

           
            await Clients.All.SendAsync("UserJoined", userName);

       
            var connectedUsers = _chatService.GetConnectedUsers();
            await Clients.All.SendAsync("UpdateUserList", connectedUsers);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User?.Identity?.Name ?? "User Unknown";
            _chatService.RemoveUser(Context.ConnectionId);

           
            await Clients.All.SendAsync("UserLeft", userName);

          
            var connectedUsers = _chatService.GetConnectedUsers();
            await Clients.All.SendAsync("UpdateUserList", connectedUsers);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string message)
        {
            var userName = Context.User?.Identity?.Name ?? "User Unknown";

            if (!string.IsNullOrWhiteSpace(message))
            {
                _chatService.AddMessage(userName, message);

                var chatMessage = new ChatMessage
                {
                    UserName = userName,
                    Message = message,
                    Timestamp = DateTime.Now,
                    IsSystemMessage = false
                };

                await Clients.All.SendAsync("ReceiveMessage", chatMessage);
            }
        }
    }
}
