using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TechShare.Data;
using TechShare.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using TechShare.Services;
var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ MVC và Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 2. Kết nối CSDL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Đăng ký Identity (User & Role) - GIỮ LẠI CÁI NÀY
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    // Cấu hình password đơn giản cho dễ test
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
// Đăng ký dịch vụ gửi email ảo
builder.Services.AddTransient<IEmailSender, EmailSender>();
// 4. Sửa lỗi đường dẫn khi chưa đăng nhập (Quan trọng cho Identity)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

var app = builder.Build();

// Cấu hình Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Kích hoạt xác thực & phân quyền
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages(); // Kích hoạt trang Login/Register

app.Run();