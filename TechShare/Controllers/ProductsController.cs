using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using TechShare.Data;
using TechShare.Models;
using TechShare.ViewModels;

namespace TechShare.Controllers
{
    public class ProductsController : Controller
    {
        private const int PageSize = 9;
        private const double NearbyRadiusKm = 50d;
        private static readonly HttpClient GeoHttpClient = BuildGeoHttpClient();
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

            var hasUserCoordinates = TryReadUserCoordinatesFromCookies(out var parsedLat, out var parsedLng);
            double? userLat = hasUserCoordinates ? parsedLat : null;
            double? userLng = hasUserCoordinates ? parsedLng : null;
            var missingNearbyLocation = string.Equals(sortOrder, "nearby", StringComparison.OrdinalIgnoreCase) && !hasUserCoordinates;

            var productsQuery = BuildCatalogQuery(searchString, categoryId, sortOrder, userLat, userLng);
            var totalItems = await productsQuery.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)PageSize);

            var productsList = await productsQuery
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

            // TÃƒÂ­nh khoÃ¡ÂºÂ£ng cÃƒÂ¡ch nÃ¡ÂºÂ¿u cÃƒÂ³ cookie
            if (userLat.HasValue && userLng.HasValue)
            {
                foreach (var product in productsList)
                {
                    if (product.Latitude.HasValue && product.Longitude.HasValue)
                    {
                        var distanceKm = CalculateApproxDistanceKm(product.Latitude.Value, product.Longitude.Value, userLat.Value, userLng.Value);
                        product.DistanceKm = Math.Round(distanceKm, 1);
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
                MissingNearbyLocation = missingNearbyLocation,
                NearbyRadiusKm = NearbyRadiusKm,
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
            if (!IsValidCoordinatePair(lat, lng))
            {
                return BadRequest(new { message = "Toa do vi tri khong hop le." });
            }

            var kmPerLat = 111.32d;
            var kmPerLng = 111.32d * Math.Cos(lat * Math.PI / 180d);
            var radiusKmSquared = NearbyRadiusKm * NearbyRadiusKm;

            var nearby = await _context.Products
                .Where(p => p.IsAvailable && p.Latitude.HasValue && p.Longitude.HasValue)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.PricePerDay,
                    p.ImageUrl,
                    p.Location,
                    DistanceSquared =
                        ((p.Latitude!.Value - lat) * kmPerLat) * ((p.Latitude!.Value - lat) * kmPerLat) +
                        ((p.Longitude!.Value - lng) * kmPerLng) * ((p.Longitude!.Value - lng) * kmPerLng)
                })
                .Where(p => p.DistanceSquared <= radiusKmSquared)
                .OrderBy(p => p.DistanceSquared)
                .Take(4)
                .ToListAsync();

            var result = nearby.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.PricePerDay,
                image = p.ImageUrl,
                location = p.Location,
                distanceKm = Math.Round(Math.Sqrt(p.DistanceSquared), 1)
            });

            return Json(result);
        }

        // DÃƒÂ nh cho Admin quÃ¡ÂºÂ£n lÃƒÂ½ TOÃƒâ‚¬N BÃ¡Â»Ëœ sÃ¡ÂºÂ£n phÃ¡ÂºÂ©m
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

        // Trang quÃ¡ÂºÂ£n lÃƒÂ½ sÃ¡ÂºÂ£n phÃ¡ÂºÂ©m cho thuÃƒÂª cÃ¡Â»Â§a cÃƒÂ¡ nhÃƒÂ¢n (USER)
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

            // TÃƒÂ­nh khoÃ¡ÂºÂ£ng cÃƒÂ¡ch nÃ¡ÂºÂ¿u cÃƒÂ³ cookie
            if (TryReadUserCoordinatesFromCookies(out var userLat, out var userLng))
            {
                if (product.Latitude.HasValue && product.Longitude.HasValue)
                {
                    var distanceKm = CalculateApproxDistanceKm(product.Latitude.Value, product.Longitude.Value, userLat, userLng);
                    product.DistanceKm = Math.Round(distanceKm, 1);
                }
            }

            return View(product);
        }

        [Authorize] // CÃ¡ÂºÂ£ User vÃƒÂ  Admin Ã„â€˜Ã¡Â»Âu cÃƒÂ³ thÃ¡Â»Æ’ thÃƒÂªm
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,PricePerDay,Location,Latitude,Longitude,IsAvailable,CategoryId")] Product product, IFormFile imageFile)
        {
            ApplyInvariantCoordinates(product);

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
                TempData["Message"] = "Da dang san pham cho thue thanh cong!";

                if (User.IsInRole("Admin")) return RedirectToAction(nameof(Manage));
                return RedirectToAction(nameof(MyProducts));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize] // Edit cho ngÃ†Â°Ã¡Â»Âi dÃƒÂ¹ng (chÃ¡Â»â€° sÃ¡Â»Â­a cÃ¡Â»Â§a mÃƒÂ¬nh) hoÃ¡ÂºÂ·c Admin (sÃ¡Â»Â­a tÃ¡ÂºÂ¥t cÃ¡ÂºÂ£)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && product.OwnerId != userId) return Forbid(); // KhÃƒÂ´ng cho sÃ¡Â»Â­a bÃƒÂ i ngÃ†Â°Ã¡Â»Âi khÃƒÂ¡c

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,PricePerDay,ImageUrl,Location,Latitude,Longitude,IsAvailable,CategoryId,OwnerId")] Product product, IFormFile imageFile)
        {
            if (id != product.Id) return NotFound();

            var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingProduct == null) return NotFound();

            ApplyInvariantCoordinates(product);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && existingProduct.OwnerId != userId) return Forbid();

            // Ã„ÂÃ¡ÂºÂ£m bÃ¡ÂºÂ£o khÃƒÂ´ng Ã„â€˜Ã¡Â»â€¢i OwnerId cÃ¡Â»Â§a ngÃ†Â°Ã¡Â»Âi khÃƒÂ¡c
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
                product.ImageUrl = existingProduct.ImageUrl; // GiÃ¡Â»Â¯ nguyÃƒÂªn Ã¡ÂºÂ£nh cÃ…Â© nÃ¡ÂºÂ¿u ko up Ã¡ÂºÂ£nh mÃ¡Â»â€ºi
            }

            ModelState.Remove("imageFile");
            ModelState.Remove("Category");
            ModelState.Remove("Owner");

            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Da cap nhat san pham thanh cong!";

                if (isAdmin) return RedirectToAction(nameof(Manage));
                return RedirectToAction(nameof(MyProducts));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [AllowAnonymous]
        [HttpGet("api/location/fallback")]
        public async Task<IActionResult> FallbackLocationByIp()
        {
            var providers = new[]
            {
                "https://ipapi.co/json/",
                "https://ipwho.is/",
                "https://ipinfo.io/json"
            };

            foreach (var providerUrl in providers)
            {
                try
                {
                    using var response = await GeoHttpClient.GetAsync(providerUrl);
                    if (!response.IsSuccessStatusCode) continue;

                    var json = await response.Content.ReadAsStringAsync();
                    if (TryParseIpGeoJson(json, out var lat, out var lng, out var area))
                    {
                        return Json(new { lat, lng, area, source = "ip" });
                    }
                }
                catch
                {
                    // Try next provider
                }
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Khong lay duoc vi tri theo IP." });
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
                TempData["Message"] = "Da xoa san pham thanh cong.";

            }

            if (isAdmin) return RedirectToAction(nameof(Manage));
            return RedirectToAction(nameof(MyProducts));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private IQueryable<Product> BuildCatalogQuery(string? searchString, int? categoryId, string? sortOrder, double? userLat, double? userLng)
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

            if (sortOrder == "nearby")
            {
                if (!userLat.HasValue || !userLng.HasValue)
                {
                    return products.Where(_ => false).OrderByDescending(p => p.Id);
                }

                var lat = userLat.Value;
                var lng = userLng.Value;
                var kmPerLat = 111.32d;
                var kmPerLng = 111.32d * Math.Cos(lat * Math.PI / 180d);
                var radiusKmSquared = NearbyRadiusKm * NearbyRadiusKm;

                return products
                    .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
                    .Where(p =>
                        p.Latitude.HasValue &&
                        p.Longitude.HasValue &&
                        (((p.Latitude.Value - lat) * kmPerLat) * ((p.Latitude.Value - lat) * kmPerLat) +
                         ((p.Longitude.Value - lng) * kmPerLng) * ((p.Longitude.Value - lng) * kmPerLng)) <= radiusKmSquared)
                    .OrderBy(p =>
                        p.Latitude.HasValue && p.Longitude.HasValue
                            ? ((p.Latitude.Value - lat) * kmPerLat) * ((p.Latitude.Value - lat) * kmPerLat) +
                              ((p.Longitude.Value - lng) * kmPerLng) * ((p.Longitude.Value - lng) * kmPerLng)
                            : radiusKmSquared)
                    .ThenByDescending(p => p.Id);
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

        private static HttpClient BuildGeoHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(6)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TechShare/1.0 (Location Fallback)");
            return client;
        }

        private static bool TryParseIpGeoJson(string json, out double lat, out double lng, out string area)
        {
            lat = 0;
            lng = 0;
            area = string.Empty;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var hasLat = TryReadDouble(root, "latitude", out lat) || TryReadDouble(root, "lat", out lat);
            var hasLng = TryReadDouble(root, "longitude", out lng) || TryReadDouble(root, "lon", out lng);

            if (!hasLat || !hasLng)
            {
                if (root.TryGetProperty("loc", out var locElement) && locElement.ValueKind == JsonValueKind.String)
                {
                    var loc = locElement.GetString();
                    var parts = loc?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts is { Length: 2 } &&
                        double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                        double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
                    {
                        hasLat = true;
                        hasLng = true;
                    }
                }
            }

            if (!hasLat || !hasLng)
            {
                return false;
            }

            var city = TryReadString(root, "city");
            var region = TryReadString(root, "region");
            var country = TryReadString(root, "country_name") ?? TryReadString(root, "country");

            area = string.Join(", ", new[] { city, region, country }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return true;
        }

        private static bool TryReadDouble(JsonElement root, string propertyName, out double value)
        {
            value = 0;
            if (!root.TryGetProperty(propertyName, out var element)) return false;

            if (element.ValueKind == JsonValueKind.Number)
            {
                return element.TryGetDouble(out value);
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }

            return false;
        }

        private bool TryReadUserCoordinatesFromCookies(out double lat, out double lng)
        {
            lat = 0;
            lng = 0;

            if (!Request.Cookies.TryGetValue("user_lat", out var latRaw) ||
                !Request.Cookies.TryGetValue("user_lng", out var lngRaw))
            {
                return false;
            }

            if (!TryParseCoordinate(latRaw, out lat) || !TryParseCoordinate(lngRaw, out lng))
            {
                return false;
            }

            return IsValidCoordinatePair(lat, lng);
        }

        private void ApplyInvariantCoordinates(Product product)
        {
            var latRaw = Request.Form["Latitude"].FirstOrDefault();
            var lngRaw = Request.Form["Longitude"].FirstOrDefault();

            if (TryParseCoordinate(latRaw, out var lat) && lat >= -90 && lat <= 90)
            {
                product.Latitude = lat;
            }

            if (TryParseCoordinate(lngRaw, out var lng) && lng >= -180 && lng <= 180)
            {
                product.Longitude = lng;
            }
        }

        private static bool TryParseCoordinate(string? raw, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static bool IsValidCoordinatePair(double lat, double lng)
        {
            return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180;
        }

        private static double CalculateApproxDistanceKm(double fromLat, double fromLng, double toLat, double toLng)
        {
            var deltaLatKm = (fromLat - toLat) * 111.32d;
            var deltaLngKm = (fromLng - toLng) * (111.32d * Math.Cos(toLat * Math.PI / 180d));
            return Math.Sqrt(deltaLatKm * deltaLatKm + deltaLngKm * deltaLngKm);
        }

        private static string? TryReadString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = element.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
