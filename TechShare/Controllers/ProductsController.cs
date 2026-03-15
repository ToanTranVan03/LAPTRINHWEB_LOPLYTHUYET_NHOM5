using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;
using TechShare.ViewModels;

namespace TechShare.Controllers
{
    public class ProductsController : Controller
    {
        private const int PageSize = 9;
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchString, int? categoryId, string? sortOrder, int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            var productsQuery = BuildCatalogQuery(searchString, categoryId, sortOrder);
            var totalItems = await productsQuery.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)PageSize);

            var productsList = await productsQuery
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

            // Tính khoảng cách nếu có cookie
            if (Request.Cookies.TryGetValue("user_lat", out var latStr) &&
                Request.Cookies.TryGetValue("user_lng", out var lngStr) &&
                double.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLat) &&
                double.TryParse(lngStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLng))
            {
                foreach (var product in productsList)
                {
                    if (product.Latitude.HasValue && product.Longitude.HasValue)
                    {
                        var distanceRaw = Math.Sqrt(Math.Pow(product.Latitude.Value - userLat, 2) + Math.Pow(product.Longitude.Value - userLng, 2));
                        product.DistanceKm = Math.Round(distanceRaw * 111.32, 1);
                    }
                }
            }

            var viewModel = new ProductCatalogViewModel
            {
                Products = productsList,
                Categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync(),
                SearchString = searchString,
                CategoryId = categoryId,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        [AllowAnonymous]
        [HttpGet("api/products/search-suggestions")]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var lowercaseQuery = query.ToLower();
            var suggestions = await _context.Products
                .Where(p => p.Name.ToLower().Contains(lowercaseQuery))
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.PricePerDay,
                    image = p.ImageUrl
                })
                .Take(5)
                .ToListAsync();

            return Json(suggestions);
        }

        [AllowAnonymous]
        [HttpGet("api/products/suggest-nearby")]
        public async Task<IActionResult> SuggestNearby(double lat, double lng)
        {
            // Lấy các sản phẩm có tọa độ
            var products = await _context.Products
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue && p.IsAvailable)
                .ToListAsync();

            if (!products.Any()) return Json(new List<object>());

            // Tính khoảng cách (Euclidean cơ bản trên map cho mục đích demo)
            var nearby = products
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.PricePerDay,
                    image = p.ImageUrl,
                    location = p.Location,
                    distanceRaw = Math.Sqrt(Math.Pow(p.Latitude!.Value - lat, 2) + Math.Pow(p.Longitude!.Value - lng, 2))
                })
                .OrderBy(p => p.distanceRaw) // Gần nhất
                .Take(4)
                .Select(p => new 
                {
                    id = p.id,
                    name = p.name,
                    price = p.price,
                    image = p.image,
                    location = p.location,
                    distanceKm = Math.Round(p.distanceRaw * 111.32, 1) // Convert tọa độ độ sang Km cơ bản
                })
                .ToList();

            return Json(nearby);
        }

        // Dành cho Admin quản lý TOÀN BỘ sản phẩm
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(products);
        }

        // Trang quản lý sản phẩm cho thuê của cá nhân (USER)
        [Authorize]
        public async Task<IActionResult> MyProducts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var myProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.OwnerId == userId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(myProducts);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .Include(p => p.Reviews!)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // Tính khoảng cách nếu có cookie
            if (Request.Cookies.TryGetValue("user_lat", out var latStr) &&
                Request.Cookies.TryGetValue("user_lng", out var lngStr) &&
                double.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLat) &&
                double.TryParse(lngStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var userLng))
            {
                if (product.Latitude.HasValue && product.Longitude.HasValue)
                {
                    var distanceRaw = Math.Sqrt(Math.Pow(product.Latitude.Value - userLat, 2) + Math.Pow(product.Longitude.Value - userLng, 2));
                    product.DistanceKm = Math.Round(distanceRaw * 111.32, 1);
                }
            }

            return View(product);
        }

        [Authorize] // Cả User và Admin đều có thể thêm
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,PricePerDay,Location,IsAvailable,CategoryId")] Product product, IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = DateTime.Now.Ticks + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                product.ImageUrl = "/images/" + fileName;
            }
            else
            {
                product.ImageUrl = "https://via.placeholder.com/300?text=No+Image";
            }
            
            product.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ModelState.Remove("ImageUrl");
            ModelState.Remove("Category");
            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Đã đăng sản phẩm cho thuê thành công!";
                if (User.IsInRole("Admin")) return RedirectToAction(nameof(Manage));
                return RedirectToAction(nameof(MyProducts));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize] // Edit cho người dùng (chỉ sửa của mình) hoặc Admin (sửa tất cả)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && product.OwnerId != userId) return Forbid(); // Không cho sửa bài người khác

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,PricePerDay,ImageUrl,Location,IsAvailable,CategoryId,OwnerId")] Product product, IFormFile imageFile)
        {
            if (id != product.Id) return NotFound();

            var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingProduct == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && existingProduct.OwnerId != userId) return Forbid();

            // Đảm bảo không đổi OwnerId của người khác
            if (!isAdmin) product.OwnerId = existingProduct.OwnerId;

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = DateTime.Now.Ticks + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                product.ImageUrl = "/images/" + fileName;
            }
            else
            {
                product.ImageUrl = existingProduct.ImageUrl; // Giữ nguyên ảnh cũ nếu ko up ảnh mới
            }

            ModelState.Remove("imageFile");
            ModelState.Remove("Category");
            ModelState.Remove("Owner");

            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Đã cập nhật sản phẩm thành công!";
                if (isAdmin) return RedirectToAction(nameof(Manage));
                return RedirectToAction(nameof(MyProducts));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && product.OwnerId != userId) return Forbid();

            return View(product);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (product != null)
            {
                if (!isAdmin && product.OwnerId != userId) return Forbid();
                
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã xóa sản phẩm thành công.";
            }

            if (isAdmin) return RedirectToAction(nameof(Manage));
            return RedirectToAction(nameof(MyProducts));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private IQueryable<Product> BuildCatalogQuery(string? searchString, int? categoryId, string? sortOrder)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString)));
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            return sortOrder switch
            {
                "price_asc" => products.OrderBy(p => p.PricePerDay),
                "price_desc" => products.OrderByDescending(p => p.PricePerDay),
                "rating" => products.OrderByDescending(p => p.Reviews != null && p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                "newest" => products.OrderByDescending(p => p.Id),
                _ => products.OrderByDescending(p => p.Id)
            };
        }
    }
}
