using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;

namespace TechShare.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // CÁCH SỬA LỖI SQL TẠI ĐÂY: 
            // Thay vì dùng danh sách trung gian và .Contains(), ta dùng .Any() truy vấn trực tiếp.
            // Câu lệnh này tương thích 100% với các bản SQL Server đời cũ.
            var chatUsers = await _userManager.Users
                .Where(u => _context.ChatMessages.Any(m => 
                    (m.SenderId == currentUserId && m.ReceiverId == u.Id) || 
                    (m.ReceiverId == currentUserId && m.SenderId == u.Id)))
                .ToListAsync();

            ApplicationUser? activeUser = null;
            if (!string.IsNullOrEmpty(userId))
            {
                activeUser = await _userManager.FindByIdAsync(userId);
                
                // Nếu đang bấm vào chat với một người mới tinh (chưa có trong lịch sử)
                if (activeUser != null && !chatUsers.Any(u => u.Id == userId))
                {
                    chatUsers.Add(activeUser);
                }
            }

            ViewBag.ActiveUserId = userId;
            ViewBag.ChatUsers = chatUsers;

            if (activeUser != null)
            {
                var messages = await _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) || 
                                (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();
                
                // Đánh dấu đã đọc các tin nhắn người kia gửi cho mình
                var unreadMsgs = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
                if (unreadMsgs.Any())
                {
                    foreach (var m in unreadMsgs) m.IsRead = true;
                    await _context.SaveChangesAsync();
                }

                ViewBag.Messages = messages;
            }

            return View();
        }
    }
}