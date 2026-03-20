using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechShare.Data;
using TechShare.Models;
using TechShare.ViewModels;

namespace TechShare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeLandingViewModel
        {
            HighlightCategories = await _context.Categories
                .Select(c => new CategoryHighlightViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductCount = c.Products!.Count()
                })
                .OrderByDescending(c => c.ProductCount)
                .ThenBy(c => c.Name)
                .Take(8)
                .ToListAsync(),
            TotalProducts = await _context.Products.CountAsync(),
            TotalCategories = await _context.Categories.CountAsync(),
            TotalReviews = await _context.Reviews.CountAsync(),
            TotalUsers = await _userManager.Users.CountAsync(),
            TotalBookings = await _context.Bookings.CountAsync(),
            LatestProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => p.IsAvailable)
                .OrderByDescending(p => p.Id)
                .Take(3)
                .ToListAsync()
        };

        return View(viewModel);
    }

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
            return Content($"Da cap quyen Admin cho: {user.UserName}");
        }

        return Content("Ban chua dang nhap!");
    }

    // Trang FAQ
    [AllowAnonymous]
    public IActionResult FAQ()
    {
        return View();
    }

    // Trang Về chúng tôi
    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalRevenue = await _context.Bookings
            .Where(b => b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed)
            .SumAsync(b => b.TotalAmount);
        ViewBag.TotalOrders = await _context.Bookings.CountAsync();
        ViewBag.TotalProducts = await _context.Products.CountAsync();
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.PendingOrders = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);
        ViewBag.TotalContacts = await _context.ContactMessages.CountAsync(c => c.Status == ContactStatus.New);

        var monthlyRevenue = new List<object>();
        for (var i = 5; i >= 0; i--)
        {
            var month = DateTime.Now.AddMonths(-i);
            var revenue = await _context.Bookings
                .Where(b => b.BookingDate.Month == month.Month && b.BookingDate.Year == month.Year)
                .Where(b => b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
            monthlyRevenue.Add(new { Label = month.ToString("MM/yyyy"), Value = revenue });
        }

        ViewBag.MonthlyRevenue = monthlyRevenue;
        ViewBag.RecentOrders = await _context.Bookings
            .Include(b => b.Product)
            .Include(b => b.Renter)
            .OrderByDescending(b => b.BookingDate)
            .Take(5)
            .ToListAsync();

        var userRoles = new Dictionary<string, bool>();
        var allUsers = await _userManager.Users.ToListAsync();
        foreach (var user in allUsers)
        {
            userRoles[user.Email ?? user.UserName ?? "N/A"] = await _userManager.IsInRoleAsync(user, "Admin");
        }

        ViewBag.UserRoles = userRoles;
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> PromoteAdmin(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["Message"] = $"Da thang chuc {email} thanh Quan tri vien!";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DemoteAdmin(string email)
    {
        const string superAdminEmail = "ttoan17123@gmail.com";

        if (email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "Loi bao mat: khong the giang cap Quan tri vien toi cao!";
            return RedirectToAction(nameof(Dashboard));
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
        {
            if (user.UserName == User.Identity?.Name)
            {
                TempData["Message"] = "Loi: ban khong the tu giang cap chinh minh!";
                return RedirectToAction(nameof(Dashboard));
            }

            await _userManager.RemoveFromRoleAsync(user, "Admin");
            TempData["Message"] = $"Da giang cap {email} ve tai khoan thuong.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        return View(BuildUserProfileViewModel(user));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError(nameof(model.FullName), "Vui lòng nhập họ và tên.");
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = BuildUserProfileViewModel(user);
            invalidViewModel.FullName = model.FullName;
            invalidViewModel.PhoneNumber = model.PhoneNumber;
            invalidViewModel.Address = model.Address;
            return View(invalidViewModel);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var failedViewModel = BuildUserProfileViewModel(user);
            failedViewModel.FullName = model.FullName;
            failedViewModel.PhoneNumber = model.PhoneNumber;
            failedViewModel.Address = model.Address;
            return View(failedViewModel);
        }

        TempData["Message"] = "Đã lưu thông tin cá nhân.";
        return RedirectToAction(nameof(Profile));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    [HttpPost]
    public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
    {
        if (avatarFile != null && avatarFile.Length > 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Lưu vào thư mục wwwroot/avatars
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                
                // Lưu ảnh với tên là ID của người dùng
                var filePath = Path.Combine(uploadsFolder, user.Id + ".jpg");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }
                TempData["Message"] = "Cập nhật ảnh đại diện thành công!";
            }
        }
        else
        {
            TempData["Message"] = "Vui lòng chọn một bức ảnh.";
        }
        
        // Trả về trang cũ
        return Redirect(Request.Headers["Referer"].ToString() ?? "/");
    }

    private UserProfileViewModel BuildUserProfileViewModel(ApplicationUser user)
    {
        double? currentLatitude = null;
        double? currentLongitude = null;
        if (Request.Cookies.TryGetValue("user_lat", out var latStr) &&
            Request.Cookies.TryGetValue("user_lng", out var lngStr) &&
            double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
        {
            currentLatitude = lat;
            currentLongitude = lng;
        }

        var displayName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.UserName ?? user.Email ?? "User"
            : user.FullName;

        return new UserProfileViewModel
        {
            FullName = displayName,
            UserNameOrEmail = user.UserName ?? user.Email ?? "N/A",
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            IsVerified = user.IsVerified,
            AvatarUrl = $"/avatars/{user.Id}.jpg?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            CurrentLatitude = currentLatitude,
            CurrentLongitude = currentLongitude
        };
    }
}
