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


// ==========================================================
    // --- 1. HÀM DASHBOARD (CẬP NHẬT: LẤY THÊM DANH SÁCH USER) ---
    // ==========================================================
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalRevenue = await _context.Bookings.Where(b => b.Status == BookingStatus.Approved).SumAsync(b => b.TotalAmount);
        ViewBag.TotalOrders = await _context.Bookings.CountAsync();
        ViewBag.TotalProducts = await _context.Products.CountAsync();
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();

        // LẬP DANH SÁCH USER VÀ KIỂM TRA QUYỀN
        var userRoles = new Dictionary<string, bool>();
        var allUsers = await _userManager.Users.ToListAsync();
        foreach (var u in allUsers)
        {
            // Kiểm tra xem user này có phải Admin không (True/False)
            userRoles[u.Email] = await _userManager.IsInRoleAsync(u, "Admin");
        }
        // Gửi danh sách này sang View
        ViewBag.UserRoles = userRoles;

        return View();
    }

    // ==========================================================
    // --- 2. HÀM THĂNG CHỨC (CẬP NHẬT: THÔNG BÁO MƯỢT HƠN) ---
    // ==========================================================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> PromoteAdmin(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["Message"] = $"✅ Đã thăng chức {email} thành Quản trị viên!";
        }
        return RedirectToAction(nameof(Dashboard)); // Load lại trang Dashboard
    }

// ==========================================================
    // --- 3. HÀM GIÁNG CẤP (ĐÃ THÊM BẢO VỆ SUPER ADMIN) ---
    // ==========================================================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DemoteAdmin(string email)
    {
        // 1. TƯỜNG LỬA: Bảo vệ Super Admin (Tài khoản gốc của bạn)
        string superAdminEmail = "ttoan17123@gmail.com"; // <-- Sửa email của bạn ở đây nếu cần

        if (email.ToLower() == superAdminEmail.ToLower())
        {
            TempData["Message"] = "❌ Lỗi bảo mật: Không ai có quyền giáng cấp Quản trị viên tối cao!";
            return RedirectToAction(nameof(Dashboard));
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
        {
            // 2. Không cho phép tự giáng cấp chính mình
            if (user.UserName == User.Identity.Name) 
            {
                TempData["Message"] = "❌ Lỗi: Bạn không thể tự giáng cấp chính mình!";
                return RedirectToAction(nameof(Dashboard));
            }

            await _userManager.RemoveFromRoleAsync(user, "Admin");
            TempData["Message"] = $"⬇️ Đã giáng cấp {email} về Khách hàng thường.";
        }
        return RedirectToAction(nameof(Dashboard));
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