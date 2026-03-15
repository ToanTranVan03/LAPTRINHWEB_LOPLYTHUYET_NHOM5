using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;

namespace TechShare.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Mock VNPay Redirect
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings.Include(b => b.Product).FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.RenterId != userId)
            {
                return NotFound();
            }

            if (booking.IsDepositPaid)
            {
                TempData["Message"] = "Đơn này đã được đặt cọc!";
                return RedirectToAction("MyBookings", "Bookings");
            }

            // Giao diện mock VNPAY
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessMockPayment(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.RenterId != userId)
            {
                return NotFound();
            }

            // Xử lý thanh toán thành công
            booking.IsDepositPaid = true;
            booking.PaymentMethod = "VNPay (Demo)";
            booking.Status = Models.BookingStatus.Approved; // Tự động duyệt khi nhận cọc

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Đã thanh toán thành công {booking.TotalAmount.ToString("N0")}đ qua VNPay Demo. Đơn thuê đã được chuyển sang trạng thái Phê duyệt.";
            return RedirectToAction("Details", "Bookings", new { id = bookingId });
        }
    }
}
