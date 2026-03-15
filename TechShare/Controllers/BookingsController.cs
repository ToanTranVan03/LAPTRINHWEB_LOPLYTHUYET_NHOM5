using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechShare.Data;
using TechShare.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TechShare.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bookings (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Product)
                .Include(b => b.Renter)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
            return View(bookings);
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Product)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create(int? productId)
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", productId);
            
            // Nếu có productId, lấy thông tin sản phẩm để hiển thị
            if (productId.HasValue)
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == productId);
                ViewBag.SelectedProduct = product;
            }
            
            return View();
        }

        // POST: Bookings/Create - TỰ TÍNH TIỀN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartDate,EndDate,ProductId")] Booking booking)
        {
            // 1. Tự động lấy ID người dùng
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            booking.RenterId = currentUserId;
            booking.BookingDate = DateTime.Now;
            booking.Status = BookingStatus.Pending;

            // Validate ngày
            if (booking.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "Ngày bắt đầu không được là ngày quá khứ!");
            }
            if (booking.EndDate <= booking.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu!");
            }

            // 2. TỰ TÍNH TIỀN: PricePerDay × số ngày
            var product = await _context.Products.FindAsync(booking.ProductId);
            if (product != null)
            {
                int totalDays = (booking.EndDate - booking.StartDate).Days;
                if (totalDays < 1) totalDays = 1;
                booking.TotalAmount = product.PricePerDay * totalDays;
            }

            // Bỏ qua check lỗi các trường tự động điền
            ModelState.Remove("Renter");
            ModelState.Remove("RenterId");
            ModelState.Remove("BookingDate");
            ModelState.Remove("Status");
            ModelState.Remove("TotalAmount");

            // 3. CHECK TRÙNG LỊCH
            var conflict = await _context.Bookings
                .Where(b => b.ProductId == booking.ProductId)
                .Where(b => b.Status != BookingStatus.Cancelled)
                .Where(b => b.StartDate < booking.EndDate && booking.StartDate < b.EndDate)
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                ModelState.AddModelError("", $"Rất tiếc! Thiết bị này đã bị thuê từ {conflict.StartDate:dd/MM} đến {conflict.EndDate:dd/MM}.");
            }

            // 4. Lưu Database
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Message"] = "✅ Đặt thuê thành công! Vui lòng chờ Admin duyệt.";
                return RedirectToAction(nameof(MyBookings));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", booking.ProductId);
            if (booking.ProductId > 0)
            {
                ViewBag.SelectedProduct = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == booking.ProductId);
            }
            return View(booking);
        }

        // GET: Bookings/Edit/5 (Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", booking.ProductId);
            ViewData["RenterId"] = new SelectList(_context.Users, "Id", "Id", booking.RenterId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BookingDate,StartDate,EndDate,TotalAmount,Status,ProductId,RenterId")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", booking.ProductId);
            ViewData["RenterId"] = new SelectList(_context.Users, "Id", "Id", booking.RenterId);
            return View(booking);
        }

        // GET: Bookings/Delete/5 (Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Product)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DUYỆT ĐƠN → Tự đổi sản phẩm thành "Đang bận"
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = BookingStatus.Approved;
                
                // Tự đổi sản phẩm thành không khả dụng
                var product = await _context.Products.FindAsync(booking.ProductId);
                if (product != null) product.IsAvailable = false;
                
                await _context.SaveChangesAsync();
                TempData["Message"] = "✅ Đã duyệt đơn hàng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // HỦY ĐƠN → Tự đổi sản phẩm thành "Sẵn sàng"
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = BookingStatus.Cancelled;
                
                // Tự đổi sản phẩm thành khả dụng lại
                var product = await _context.Products.FindAsync(booking.ProductId);
                if (product != null) product.IsAvailable = true;
                
                await _context.SaveChangesAsync();
                TempData["Message"] = "⬇️ Đã hủy đơn hàng.";
            }
            return RedirectToAction(nameof(Index));
        }

        // HOÀN THÀNH ĐƠN → Sản phẩm sẵn sàng lại
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Complete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = BookingStatus.Completed;
                
                var product = await _context.Products.FindAsync(booking.ProductId);
                if (product != null) product.IsAvailable = true;
                
                await _context.SaveChangesAsync();
                TempData["Message"] = "🎉 Đơn hàng đã hoàn thành!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // TRANG LỊCH SỬ CÁ NHÂN
        public async Task<IActionResult> MyBookings()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myList = await _context.Bookings
                .Include(b => b.Product)
                .Where(b => b.RenterId == currentUserId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(myList);
        }
    }
}