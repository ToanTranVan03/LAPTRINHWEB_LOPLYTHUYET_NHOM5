using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechShare.Data;
using TechShare.Models;
using System.Security.Claims; // Thư viện lấy ID người dùng
using Microsoft.AspNetCore.Authorization;

namespace TechShare.Controllers
{
    [Authorize] // Yêu cầu: Phải đăng nhập mới được vào class này
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bookings
        // --- CHỈ ADMIN MỚI ĐƯỢC XEM DANH SÁCH TẤT CẢ ---
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Bookings.Include(b => b.Product).Include(b => b.Renter);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Bookings/Details/5
        // (Không khóa Admin để khách có thể xem chi tiết đơn của mình từ trang MyBookings)
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
        public IActionResult Create(int? productId) 
        {
            // Nếu có productId truyền vào thì chọn sẵn, không thì để null
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", productId);
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartDate,EndDate,TotalAmount,ProductId")] Booking booking)
        {
            // 1. Tự động lấy ID người dùng
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            booking.RenterId = currentUserId;
            
            // 2. Tự động điền ngày tạo đơn
            booking.BookingDate = DateTime.Now; 
            
            // 3. Tự động điền trạng thái Pending
            booking.Status = BookingStatus.Pending; 

            // Bỏ qua check lỗi các trường tự động điền
            ModelState.Remove("Renter");
            ModelState.Remove("RenterId");
            ModelState.Remove("BookingDate");
            ModelState.Remove("Status");

            // 4. CHECK TRÙNG LỊCH (Logic thông minh)
            var conflict = await _context.Bookings
                .Where(b => b.ProductId == booking.ProductId)
                .Where(b => b.Status != BookingStatus.Cancelled) // Bỏ qua đơn đã hủy
                .Where(b => b.StartDate < booking.EndDate && booking.StartDate < b.EndDate)
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                ModelState.AddModelError("", $"Rất tiếc! Thiết bị này đã bị thuê từ {conflict.StartDate:dd/MM} đến {conflict.EndDate:dd/MM}.");
            }

            // 5. Lưu Database
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyBookings)); // Tạo xong chuyển về trang Lịch sử của tôi
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", booking.ProductId);
            return View(booking);
        }

        // GET: Bookings/Edit/5
        // --- CHỈ ADMIN MỚI ĐƯỢC SỬA ĐƠN HÀNG ---
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
        [Authorize(Roles = "Admin")] // --- CHỈ ADMIN MỚI ĐƯỢC LƯU SỬA ---
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

        // GET: Bookings/Delete/5
        // --- CHỈ ADMIN MỚI ĐƯỢC XÓA ---
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
        [Authorize(Roles = "Admin")] // --- CHỈ ADMIN MỚI ĐƯỢC XÁC NHẬN XÓA ---
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
        // --- CHỨC NĂNG DUYỆT ĐƠN HÀNG (CHỈ ADMIN) ---

// 1. Duyệt đơn (Approve)
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Approve(int id)
{
    var booking = await _context.Bookings.FindAsync(id);
    if (booking != null)
    {
        booking.Status = BookingStatus.Approved; // Chuyển sang Đã duyệt
        await _context.SaveChangesAsync();
    }
    return RedirectToAction(nameof(Index));
}

// 2. Hủy đơn (Reject/Cancel)
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Reject(int id)
{
    var booking = await _context.Bookings.FindAsync(id);
    if (booking != null)
    {
        booking.Status = BookingStatus.Cancelled; // Chuyển sang Đã hủy
        await _context.SaveChangesAsync();
    }
    return RedirectToAction(nameof(Index));
}
        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // --- TRANG LỊCH SỬ CÁ NHÂN (AI CŨNG VÀO ĐƯỢC ĐỂ XEM ĐƠN CỦA MÌNH) ---
        public async Task<IActionResult> MyBookings()
        {
            // 1. Lấy ID người đang đăng nhập
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Chỉ lấy những đơn hàng CỦA NGƯỜI NÀY
            var myList = await _context.Bookings
                .Include(b => b.Product) // Kèm thông tin sản phẩm
                .Where(b => b.RenterId == currentUserId) // Lọc theo ID
                .OrderByDescending(b => b.BookingDate) // Đơn mới nhất lên đầu
                .ToListAsync();

            return View(myList);
        }
    }
}