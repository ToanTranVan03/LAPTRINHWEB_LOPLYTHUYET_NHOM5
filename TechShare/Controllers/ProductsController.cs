using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechShare.Data;
using TechShare.Models;
using Microsoft.AspNetCore.Authorization; // Thư viện phân quyền
using System.IO; // Thư viện xử lý file ảnh

namespace TechShare.Controllers
{
    [Authorize(Roles = "Admin")] // Quan trọng: Chỉ Admin mới được vào trang này
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Products.Include(p => p.Category).Include(p => p.Owner);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            // Không cần chọn Owner vì sẽ tự gán là Admin
            return View();
        }

        // POST: Products/Create
        // --- ĐÂY LÀ PHẦN XỬ LÝ UPLOAD ẢNH ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,PricePerDay,Location,IsAvailable,CategoryId")] Product product, IFormFile imageFile)
        {
            // 1. Xử lý lưu ảnh nếu có file được chọn
            if (imageFile != null && imageFile.Length > 0)
            {
                // Tạo tên file độc nhất (dùng giờ phút giây hiện tại)
                var fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(imageFile.FileName);
                
                // Xác định đường dẫn lưu (vào thư mục wwwroot/images)
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                // Lưu file vật lý lên Server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Lưu đường dẫn ảo vào Database
                product.ImageUrl = "/images/" + fileName;
            }
            else
            {
                // Nếu không chọn ảnh thì dùng ảnh mặc định
                product.ImageUrl = "https://via.placeholder.com/300?text=No+Image"; 
            }

            // 2. Tự động các trường khác
            // Bỏ qua check lỗi các trường này
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Category");
            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }
// POST: Products/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,PricePerDay,ImageUrl,Location,IsAvailable,CategoryId,OwnerId")] Product product, IFormFile imageFile)
{
    if (id != product.Id) return NotFound();

    // 1. Xử lý logic thay ảnh
    if (imageFile != null && imageFile.Length > 0)
    {
        // Nếu có chọn ảnh mới -> Lưu ảnh mới đè lên
        var fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(imageFile.FileName);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        // Cập nhật đường dẫn mới
        product.ImageUrl = "/images/" + fileName;
    }
    // Nếu imageFile == null thì nó sẽ tự giữ nguyên giá trị product.ImageUrl cũ (nhờ cái input type="hidden" bên View)

    // 2. Bỏ qua check lỗi validation
    ModelState.Remove("imageFile"); // Bỏ qua file
    ModelState.Remove("Category");
    ModelState.Remove("Owner");

    if (ModelState.IsValid)
    {
        try
        {
            _context.Update(product);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(product.Id)) return NotFound();
            else throw;
        }
        return RedirectToAction(nameof(Index));
    }
    
    ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
    return View(product);
}

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}