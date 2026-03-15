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

            // Tìm những người đã từng chat với user hiện tại
            var chatHistoryUserIds = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var chatUsers = await _userManager.Users
                .Where(u => chatHistoryUserIds.Contains(u.Id))
                .ToListAsync();

            ApplicationUser? activeUser = null;
            if (!string.IsNullOrEmpty(userId))
            {
                activeUser = await _userManager.FindByIdAsync(userId);
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
                
                // Đánh dấu đã đọc
                var unreadMsgs = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
                foreach (var m in unreadMsgs) m.IsRead = true;
                if (unreadMsgs.Any()) await _context.SaveChangesAsync();

                ViewBag.Messages = messages;
            }

            return View();
        }
    }
}
