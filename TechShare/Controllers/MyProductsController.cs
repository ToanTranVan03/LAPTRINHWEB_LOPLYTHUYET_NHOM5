using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechShare.Data;
using TechShare.Models;

namespace TechShare.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới được vào
    public class MyProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MyProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. Xem danh sách thiết bị CỦA CHÍNH MÌNH
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Chỉ lấy sản phẩm có OwnerId trùng với ID của người dùng đang đăng nhập
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.OwnerId == userId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            return View(products);
        }

        // 2. Giao diện Thêm mới thiết bị
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // 3. Xử lý lưu thiết bị và tải ảnh lên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            product.OwnerId = userId; // Gán quyền sở hữu cho người đăng
            product.IsAvailable = true;

            // Bỏ qua kiểm tra lỗi các class liên kết
            ModelState.Remove("Owner");
            ModelState.Remove("Category");
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                // Xử lý lưu ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }
                else 
                {
                    product.ImageUrl = "https://placehold.co/600x400?text=No+Image"; // Ảnh mặc định nếu ko up
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã thêm thiết bị cho thuê thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 4. Chức năng XÓA (Bảo mật: Chỉ xóa được nếu đúng chủ sở hữu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Tìm sản phẩm đúng ID và BẮT BUỘC OwnerId phải là của người dùng này
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);
            
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã xóa thiết bị thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}