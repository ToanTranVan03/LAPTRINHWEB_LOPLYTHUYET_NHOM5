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
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(message))
            {
                return;
            }

            // Lưu tin nhắn vào DB
            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            // Gửi tin nhắn đến người nhận
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message, chatMsg.SentAt.ToString("HH:mm"));

            // Gửi tin nhắn cho chính mình (để hiển thị lên UI)
            await Clients.Caller.SendAsync("MessageSent", receiverId, message, chatMsg.SentAt.ToString("HH:mm"));
        }
    }
}
