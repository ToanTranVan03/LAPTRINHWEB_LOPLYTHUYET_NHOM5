using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;

namespace TechShare.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Wishlist - Danh sách yêu thích
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlists = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Reviews)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            return View(wishlists);
        }

        // POST: Wishlist/Toggle - Thêm/Xóa khỏi wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                TempData["Message"] = "Đã xóa khỏi danh sách yêu thích.";
            }
            else
            {
                _context.Wishlists.Add(new Wishlist
                {
                    UserId = userId!,
                    ProductId = productId,
                    AddedAt = DateTime.Now
                });
                TempData["Message"] = "❤️ Đã thêm vào danh sách yêu thích!";
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Products");
        }

        // API: Kiểm tra sản phẩm có trong wishlist không
        [HttpGet]
        public async Task<IActionResult> Check(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
            return Json(new { inWishlist = exists });
        }
    }
}
