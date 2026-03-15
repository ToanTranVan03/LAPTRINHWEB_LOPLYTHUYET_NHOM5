using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;

namespace TechShare.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int ProductId, int Rating, string? Comment)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var hasCompleted = await _context.Bookings
                .AnyAsync(b => b.ProductId == ProductId
                    && b.RenterId == currentUserId
                    && (b.Status == BookingStatus.Completed || b.Status == BookingStatus.Approved));

            if (!hasCompleted)
            {
                TempData["Message"] = "Bạn cần thuê và hoàn thành đơn hàng trước khi đánh giá.";
                return RedirectToAction("Details", "Products", new { id = ProductId });
            }

            var alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ProductId == ProductId && r.ReviewerId == currentUserId);

            if (alreadyReviewed)
            {
                TempData["Message"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            var review = new Review
            {
                ProductId = ProductId,
                ReviewerId = currentUserId,
                Rating = Math.Clamp(Rating, 1, 5),
                Comment = Comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Cảm ơn bạn đã đánh giá.";
            return RedirectToAction("MyBookings", "Bookings");
        }
    }
}
