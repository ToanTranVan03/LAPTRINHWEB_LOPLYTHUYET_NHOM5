using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;

namespace TechShare.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Contact - Form liên hệ
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    ViewBag.UserFullName = user.FullName;
                    ViewBag.UserEmail = user.Email;
                }
            }
            return View();
        }

        // POST: Contact - Gửi tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactMessage contactMessage)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                contactMessage.SenderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            contactMessage.CreatedAt = DateTime.Now;
            contactMessage.Status = ContactStatus.New;

            ModelState.Remove("Sender");
            ModelState.Remove("SenderId");
            ModelState.Remove("AdminReply");
            ModelState.Remove("RepliedAt");

            if (ModelState.IsValid)
            {
                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();
                TempData["Message"] = "✅ Tin nhắn đã được gửi thành công! Chúng tôi sẽ phản hồi sớm nhất.";
                return RedirectToAction(nameof(ThankYou));
            }

            return View(contactMessage);
        }

        // GET: Contact/ThankYou
        [AllowAnonymous]
        public IActionResult ThankYou()
        {
            return View();
        }

        // GET: Contact/Manage - Admin quản lý tin nhắn
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage(string? status)
        {
            var query = _context.ContactMessages
                .Include(c => c.Sender)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContactStatus>(status, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }

            var messages = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.TotalNew = await _context.ContactMessages.CountAsync(c => c.Status == ContactStatus.New);
            ViewBag.TotalRead = await _context.ContactMessages.CountAsync(c => c.Status == ContactStatus.Read);
            ViewBag.TotalReplied = await _context.ContactMessages.CountAsync(c => c.Status == ContactStatus.Replied);

            return View(messages);
        }

        // GET: Contact/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var message = await _context.ContactMessages
                .Include(c => c.Sender)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (message == null) return NotFound();

            // Đánh dấu đã đọc
            if (message.Status == ContactStatus.New)
            {
                message.Status = ContactStatus.Read;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // POST: Contact/Reply/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string adminReply)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.AdminReply = adminReply;
            message.RepliedAt = DateTime.Now;
            message.Status = ContactStatus.Replied;
            await _context.SaveChangesAsync();

            TempData["Message"] = "✅ Đã phản hồi tin nhắn thành công!";
            return RedirectToAction(nameof(Manage));
        }

        // POST: Contact/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
            }
            TempData["Message"] = "Đã xóa tin nhắn.";
            return RedirectToAction(nameof(Manage));
        }
    }
}
