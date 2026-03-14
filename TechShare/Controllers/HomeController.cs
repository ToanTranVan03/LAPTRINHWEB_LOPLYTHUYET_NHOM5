using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; 
using Microsoft.EntityFrameworkCore;
using TechShare.Data;
using TechShare.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization; // <--- Đừng quên thư viện này

namespace TechShare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public HomeController(ILogger<HomeController> logger, 
                          ApplicationDbContext context,
                          UserManager<ApplicationUser> userManager,
                          RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // 1. TRANG CHỦ (CÓ TÌM KIẾM & LỌC)
    public async Task<IActionResult> Index(string searchString, int? categoryId)
    {
        var products = from p in _context.Products.Include(p => p.Category)
                       select p;

        if (!string.IsNullOrEmpty(searchString))
        {
            products = products.Where(s => s.Name.Contains(searchString));
        }

        if (categoryId.HasValue)
        {
            products = products.Where(x => x.CategoryId == categoryId);
        }

        ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
        ViewData["CurrentFilter"] = searchString;

        return View(await products.ToListAsync());
    }

    // 2. CẤP QUYỀN ADMIN (CHẠY 1 LẦN)
    public async Task<IActionResult> SetupAdmin()
    {
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            return Content($"✅ Đã cấp quyền Admin cho: {user.UserName}");
        }
        return Content("❌ Bạn chưa đăng nhập!");
    }

    // 3. DASHBOARD THỐNG KÊ (ĐOẠN BẠN ĐANG THIẾU TRONG CODE CŨ)
    [Authorize(Roles = "Admin")] 
    public async Task<IActionResult> Dashboard()
    {
        // Doanh thu (Chỉ tính đơn đã duyệt)
        var totalRevenue = await _context.Bookings
            .Where(b => b.Status == BookingStatus.Approved)
            .SumAsync(b => b.TotalAmount);

        // Các số liệu khác
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.TotalOrders = await _context.Bookings.CountAsync();
        ViewBag.TotalProducts = await _context.Products.CountAsync();
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();

        return View();
    }
// --- HÀM THĂNG CHỨC ADMIN (CHỈ ADMIN HIỆN TẠI MỚI DÙNG ĐƯỢC) ---
    [Authorize(Roles = "Admin")]
    [HttpPost] // Hàm này nhận dữ liệu từ Form gửi lên
    public async Task<IActionResult> PromoteAdmin(string email)
    {
        // 1. Tìm người dùng theo email
        var user = await _userManager.FindByEmailAsync(email);

        // 2. Kiểm tra xem có tìm thấy không
        if (user == null)
        {
            // Nếu không thấy, báo lỗi (bạn có thể làm trang báo lỗi đẹp hơn sau này)
            return Content($"❌ Lỗi: Không tìm thấy tài khoản nào có email: {email}. Hãy bảo họ đăng ký tài khoản trước!");
        }

        // 3. Nếu tìm thấy -> Thêm vào nhóm Admin
        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            return Content($"✅ Thành công! Tài khoản {email} đã trở thành Admin. Hãy bảo họ đăng xuất và đăng nhập lại.");
        }
        else
        {
            return Content($"ℹ️ Tài khoản {email} đã là Admin từ trước rồi!");
        }
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}