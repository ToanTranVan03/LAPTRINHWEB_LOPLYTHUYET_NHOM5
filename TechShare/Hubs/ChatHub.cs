using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;

namespace TechShare.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var trimmedMessage = message.Trim();
            if (string.IsNullOrEmpty(trimmedMessage))
            {
                return;
            }

            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = trimmedMessage,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            await Clients.Users(new[] { senderId, receiverId })
                .SendAsync("ReceiveMessage", senderId, trimmedMessage, chatMsg.SentAt.ToString("HH:mm"));
        }
    }
}
