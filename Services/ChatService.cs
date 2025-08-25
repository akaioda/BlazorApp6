using BlazorApp6.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BlazorApp6.Services
{
    public class ChatService
    {
        private readonly ConcurrentDictionary<string, string> _connectedUsers = new();
        private readonly List<ChatMessage> _messages = new();
        private readonly object _messagesLock = new();
        private readonly IHubContext<ChatHub> _hubContext;

        public event Action<ChatMessage>? MessageReceived;
        public event Action<string>? UserJoined;
        public event Action<string>? UserLeft;

        public ChatService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void AddUser(string connectionId, string userName)
        {
            _connectedUsers.TryAdd(connectionId, userName);
            UserJoined?.Invoke(userName);
        }

        public void RemoveUser(string connectionId)
        {
            if (_connectedUsers.TryRemove(connectionId, out var userName))
            {
                UserLeft?.Invoke(userName);
            }
        }

        public void AddMessage(string userName, string message)
        {
            var chatMessage = new ChatMessage
            {
                UserName = userName,
                Message = message,
                Timestamp = DateTime.Now
            };

            lock (_messagesLock)
            {
                _messages.Add(chatMessage);
                // Keep last 100
                if (_messages.Count > 100)
                {
                    _messages.RemoveAt(0);
                }
            }

            MessageReceived?.Invoke(chatMessage);
        }

        public List<ChatMessage> GetRecentMessages()
        {
            lock (_messagesLock)
            {
                return _messages.ToList();
            }
        }

        public List<string> GetConnectedUsers()
        {
            return _connectedUsers.Values.Distinct().ToList();
        }

        public async Task SendServerMessageToAll(string message)
        {
            var chatMessage = new ChatMessage
            {
                UserName = "Server",
                Message = message,
                Timestamp = DateTime.Now,
                IsSystemMessage = true
            };

            lock (_messagesLock)
            {
                _messages.Add(chatMessage);
                if (_messages.Count > 100)
                {
                    _messages.RemoveAt(0);
                }
            }

            MessageReceived?.Invoke(chatMessage);
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", chatMessage);
        }

        public async Task SendServerMessageToUser(string userName, string message)
        {
            var connectionId = _connectedUsers.FirstOrDefault(x => x.Value == userName).Key;
            if (!string.IsNullOrEmpty(connectionId))
            {
                var chatMessage = new ChatMessage
                {
                    UserName = "Server (private)",
                    Message = message,
                    Timestamp = DateTime.Now,
                    IsSystemMessage = true
                };

                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", chatMessage);
            }
        }

        public async Task SendServerAnnouncement(string announcement)
        {
            var chatMessage = new ChatMessage
            {
                UserName = "Speaker",
                Message = announcement,
                Timestamp = DateTime.Now,
                IsSystemMessage = true
            };

            lock (_messagesLock)
            {
                _messages.Add(chatMessage);
                if (_messages.Count > 100)
                {
                    _messages.RemoveAt(0);
                }
            }

            MessageReceived?.Invoke(chatMessage);
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", chatMessage);
        }

        public async Task BroadcastUserCount()
        {
            var userCount = _connectedUsers.Values.Distinct().Count();
            await _hubContext.Clients.All.SendAsync("UpdateUserCount", userCount);
        }
    }

    public class ChatMessage
    {
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSystemMessage { get; set; } = false;
    }
}
