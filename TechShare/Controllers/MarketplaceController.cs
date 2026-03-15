using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;

namespace TechShare.Controllers
{
    public class MarketplaceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarketplaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Marketplace - Danh sách sản phẩm đang bán
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? categoryId, string? search, string? condition, string? sort)
        {
            var query = _context.MarketplaceListings
                .Include(m => m.Seller)
                .Include(m => m.Category)
                .Where(m => m.Status == ListingStatus.Active)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(m => m.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => m.Title.Contains(search) || m.Description.Contains(search));

            if (!string.IsNullOrEmpty(condition) && Enum.TryParse<ProductCondition>(condition, out var condEnum))
                query = query.Where(m => m.Condition == condEnum);

            query = sort switch
            {
                "price_asc" => query.OrderBy(m => m.Price),
                	"price_desc" => query.OrderByDescending(m => m.Price),
                "oldest" => query.OrderBy(m => m.CreatedAt),
                _ => query.OrderByDescending(m => m.CreatedAt)
            };

            var listings = await query.ToListAsync();

            // Tính khoảng cách nếu có cookie
            if (Request.Cookies.TryGetValue("user_lat", out var latStr) &&
                Request.Cookies.TryGetValue("user_lng", out var lngStr) &&
                double.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLat) &&
                double.TryParse(lngStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLng))
            {
                foreach (var item in listings)
                {
                    if (item.Latitude.HasValue && item.Longitude.HasValue)
                    {
                        var distanceRaw = Math.Sqrt(Math.Pow(item.Latitude.Value - userLat, 2) + Math.Pow(item.Longitude.Value - userLng, 2));
                        item.DistanceKm = Math.Round(distanceRaw * 111.32, 1);
                    }
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;
            ViewBag.Condition = condition;
            ViewBag.Sort = sort;
            ViewBag.TotalListings = listings.Count;

            return View(listings);
        }

        // GET: Marketplace/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var listing = await _context.MarketplaceListings
                .Include(m => m.Seller)
                .Include(m => m.Category)
                .Include(m => m.Buyer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (listing == null) return NotFound();

            // Tính khoảng cách nếu có cookie
            if (Request.Cookies.TryGetValue("user_lat", out var latStr) &&
                Request.Cookies.TryGetValue("user_lng", out var lngStr) &&
                double.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLat) &&
                double.TryParse(lngStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLng))
            {
                if (listing.Latitude.HasValue && listing.Longitude.HasValue)
                {
                    var distanceRaw = Math.Sqrt(Math.Pow(listing.Latitude.Value - userLat, 2) + Math.Pow(listing.Longitude.Value - userLng, 2));
                    listing.DistanceKm = Math.Round(distanceRaw * 111.32, 1);
                }
            }

            // Các tin rao bán tương tự
            ViewBag.SimilarListings = await _context.MarketplaceListings
                .Include(m => m.Category)
                .Where(m => m.Id != id && m.Status == ListingStatus.Active && m.CategoryId == listing.CategoryId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(3)
                .ToListAsync();

            return View(listing);
        }

        // GET: Marketplace/Create - Đăng bán sản phẩm
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Marketplace/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarketplaceListing listing)
        {
            listing.SellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            listing.CreatedAt = DateTime.Now;
            listing.Status = ListingStatus.Active;

            ModelState.Remove("Seller");
            ModelState.Remove("SellerId");
            ModelState.Remove("Buyer");
            ModelState.Remove("BuyerId");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                _context.MarketplaceListings.Add(listing);
                await _context.SaveChangesAsync();
                TempData["Message"] = "✅ Đăng bán thành công! Tin rao của bạn đã xuất hiện trên chợ.";
                return RedirectToAction(nameof(MyListings));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View(listing);
        }

        // GET: Marketplace/MyListings - Tin rao của tôi
        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listings = await _context.MarketplaceListings
                .Include(m => m.Category)
                .Include(m => m.Buyer)
                .Where(m => m.SellerId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(listings);
        }

        // POST: Marketplace/MarkSold/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSold(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listing = await _context.MarketplaceListings.FindAsync(id);
            if (listing == null || listing.SellerId != userId) return NotFound();

            listing.Status = ListingStatus.Sold;
            await _context.SaveChangesAsync();
            TempData["Message"] = "✅ Đã đánh dấu là đã bán!";
            return RedirectToAction(nameof(MyListings));
        }

        // POST: Marketplace/Cancel/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listing = await _context.MarketplaceListings.FindAsync(id);
            if (listing == null || listing.SellerId != userId) return NotFound();

            listing.Status = ListingStatus.Cancelled;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Đã hủy tin rao bán.";
            return RedirectToAction(nameof(MyListings));
        }

        // POST: Marketplace/Delete/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var listing = await _context.MarketplaceListings.FindAsync(id);
            if (listing == null || (!isAdmin && listing.SellerId != userId)) return NotFound();

            _context.MarketplaceListings.Remove(listing);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Đã xóa tin rao bán.";

            if (isAdmin)
                return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(MyListings));
        }
    }
}
