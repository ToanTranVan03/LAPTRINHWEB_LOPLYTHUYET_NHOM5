using Microsoft.AspNetCore.Identity;
using TechShare.Data;
using TechShare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TechShare.Services
{
    public static class SeedData
    {
        private static readonly HttpClient SeedImageHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();

            await context.Database.EnsureCreatedAsync();

            // ============================================================
            // 1. SEED ROLES
            // ============================================================
            string adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            // ============================================================
            // 2. SEED ADMIN USER
            // ============================================================
            string adminEmail = "admin@techshare.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản trị viên",
                    Address = "Nam Định, Việt Nam",
                    IsVerified = true,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }

            // Demo users
            var demoUsers = new[]
            {
                new { Email = "nguyen.van.a@demo.com", FullName = "Nguyễn Văn A", Address = "Hà Nội" },
                new { Email = "tran.thi.b@demo.com",  FullName = "Trần Thị B",   Address = "TP. HCM" },
                new { Email = "le.van.c@demo.com",    FullName = "Lê Văn C",     Address = "Đà Nẵng" },
                new { Email = "pham.thi.d@demo.com",  FullName = "Phạm Thị D",   Address = "Hải Phòng" },
                new { Email = "hoang.van.e@demo.com", FullName = "Hoàng Văn E",  Address = "Cần Thơ" },
                new { Email = "do.thi.f@demo.com",    FullName = "Đỗ Thị F",     Address = "Nha Trang" },
            };
            var createdUsers = new List<ApplicationUser> { adminUser };
            foreach (var d in demoUsers)
            {
                if (await userManager.FindByEmailAsync(d.Email) == null)
                {
                    var u = new ApplicationUser
                    {
                        UserName = d.Email, Email = d.Email,
                        FullName = d.FullName, Address = d.Address,
                        IsVerified = true, EmailConfirmed = true
                    };
                    await userManager.CreateAsync(u, "Demo@123");
                    createdUsers.Add(u);
                }
                else
                {
                    createdUsers.Add((await userManager.FindByEmailAsync(d.Email))!);
                }
            }

            // ============================================================
            // 3. SEED CATEGORIES
            // ============================================================
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new() { Name = "Laptop & Máy tính" },
                    new() { Name = "Máy ảnh & Quay phim" },
                    new() { Name = "Âm thanh & Loa" },
                    new() { Name = "Console & Gaming" },
                    new() { Name = "Máy chiếu & Màn hình" },
                    new() { Name = "Tablet & iPad" },
                    new() { Name = "Điện thoại" },
                    new() { Name = "Thiết bị mạng" },
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 4. SEED PRODUCTS (MỞ RỘNG - 30+ sản phẩm)
            // ============================================================
            if (!await context.Products.AnyAsync())
            {
                var cats = await context.Categories.ToListAsync();
                int laptop     = cats.First(c => c.Name.Contains("Laptop")).Id;
                int camera     = cats.First(c => c.Name.Contains("Máy ảnh")).Id;
                int audio      = cats.First(c => c.Name.Contains("Âm thanh")).Id;
                int gaming     = cats.First(c => c.Name.Contains("Console")).Id;
                int projector  = cats.First(c => c.Name.Contains("Máy chiếu")).Id;
                int tablet     = cats.First(c => c.Name.Contains("Tablet")).Id;
                int phone      = cats.First(c => c.Name.Contains("Điện thoại")).Id;
                int network    = cats.First(c => c.Name.Contains("Thiết bị mạng")).Id;

                var products = new List<Product>
                {
                    // === LAPTOP (6 sản phẩm) ===
                    new() {
                        Name = "MacBook Pro 16\" M3 Max",
                        Description = "Chip M3 Max 16-core, RAM 64GB, SSD 1TB. Máy mới 95%, còn bảo hành tới tháng 12/2026. Kèm sạc 140W và túi chống sốc.",
                        PricePerDay = 450_000, CategoryId = laptop,
                        Location = "Hà Nội - Cầu Giấy", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Dell XPS 15 OLED 2024",
                        Description = "Core i9-14900H, RTX 4070, RAM 32GB, màn hình OLED 3.5K 60Hz. Lý tưởng cho thiết kế đồ họa và dựng video.",
                        PricePerDay = 380_000, CategoryId = laptop,
                        Location = "TP. HCM - Quận 1", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1593642632559-0c6d3fc62b89?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "ThinkPad X1 Carbon Gen 12",
                        Description = "Core Ultra 7, RAM 32GB, nhẹ chỉ 1.12kg, pin 15h. Phù hợp đi công tác, lớp học, làm việc di động.",
                        PricePerDay = 280_000, CategoryId = laptop,
                        Location = "Đà Nẵng - Hải Châu", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "ASUS ROG Strix G16 2024",
                        Description = "Core i9-14900HX, RTX 4080, RAM 32GB, màn hình 240Hz. Laptop gaming mạnh mẽ, đèn Aura Sync RGB.",
                        PricePerDay = 400_000, CategoryId = laptop,
                        Location = "Hà Nội - Đống Đa", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "MacBook Air 15\" M3",
                        Description = "Chip M3 8-core, RAM 16GB, SSD 512GB, pin 18h sử dụng thực tế. Siêu mỏng nhẹ cho sinh viên.",
                        PricePerDay = 250_000, CategoryId = laptop,
                        Location = "TP. HCM - Quận 7", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "HP Spectre x360 14",
                        Description = "Core Ultra 7, OLED 2.8K, bản lề 360°, bút stylus. Laptop 2-in-1 cao cấp nhất HP.",
                        PricePerDay = 320_000, CategoryId = laptop,
                        Location = "Đà Nẵng - Sơn Trà", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=600&q=80",
                        OwnerId = adminUser.Id
                    },

                    // === CAMERA (5 sản phẩm) ===
                    new() {
                        Name = "Sony A7R V + Lens 24-70 f/2.8",
                        Description = "61MP Full Frame, video 8K RAW, chống rung 8-stop. Kèm lens G Master 24-70mm f/2.8, 2 pin, bộ sạc, thẻ SD 256GB.",
                        PricePerDay = 600_000, CategoryId = camera,
                        Location = "Hà Nội - Hoàn Kiếm", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1516961642265-531546e84af2?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Canon EOS R6 Mark II",
                        Description = "40fps burst, IBIS 8-stop, video 6K RAW Internal. Phù hợp chụp sự kiện, cưới, thể thao. Kèm lens 24-105mm f/4L.",
                        PricePerDay = 500_000, CategoryId = camera,
                        Location = "TP. HCM - Quận 3", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1606986628253-d1bab3b35c70?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "DJI Mavic 3 Classic + ND Filter",
                        Description = "Drone Hasselblad 5.1K, thời lượng bay 46', tránh vật cản đa hướng. Kèm bộ ND filter 4/8/16/64.",
                        PricePerDay = 700_000, CategoryId = camera,
                        Location = "Đà Nẵng - Sơn Trà", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1473968512647-3e447244af8f?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "GoPro Hero 12 Black Bundle",
                        Description = "Action cam 5.3K60fps, HyperSmooth 6.0, chống nước 10m. Kèm gậy selfie, mount mũ bảo hiểm, pin dự phòng.",
                        PricePerDay = 200_000, CategoryId = camera,
                        Location = "Nha Trang - Trần Phú", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Fujifilm X-T5 + 35mm f/1.4",
                        Description = "40.2MP APS-C, film simulation Reala Ace, quay 6.2K. Máy ảnh retro chuyên street photography.",
                        PricePerDay = 400_000, CategoryId = camera,
                        Location = "Hà Nội - Tây Hồ", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=600&q=80",
                        OwnerId = adminUser.Id
                    },

                    // === AUDIO (4 sản phẩm) ===
                    new() {
                        Name = "Bose QC Ultra Headphones",
                        Description = "ANC đỉnh cao, âm thanh Immersive, pin 24h. Hoàn hảo cho du lịch hay phòng thu.",
                        PricePerDay = 150_000, CategoryId = audio,
                        Location = "Hà Nội - Đống Đa", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "JBL PartyBox 310",
                        Description = "Loa bluetooth 240W, hiệu ứng đèn LED, mic karaoke có dây kèm theo. Lý tưởng cho party ngoài trời.",
                        PricePerDay = 250_000, CategoryId = audio,
                        Location = "TP. HCM - Bình Thạnh", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Sony WH-1000XM5",
                        Description = "Tai nghe chống ồn cao cấp, pin 30h, Multipoint 2 thiết bị. Âm thanh Hi-Res LDAC.",
                        PricePerDay = 120_000, CategoryId = audio,
                        Location = "Đà Nẵng - Thanh Khê", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Shure SM7B + Audio Interface",
                        Description = "Mic thu âm chuyên nghiệp chuẩn studio, kèm Focusrite Scarlett 2i2, cáp XLR, pop filter và chân đế.",
                        PricePerDay = 350_000, CategoryId = audio,
                        Location = "Hà Nội - Cầu Giấy", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1598488035139-bdbb2231ce04?w=600&q=80",
                        OwnerId = adminUser.Id
                    },

                    // === GAMING (4 sản phẩm) ===
                    new() {
                        Name = "PlayStation 5 Slim + 10 Game",
                        Description = "PS5 Slim bản đĩa, 1TB SSD, kèm 2 tay cầm DualSense và 10 game bản vật lý.",
                        PricePerDay = 350_000, CategoryId = gaming,
                        Location = "Hà Nội - Cầu Giấy", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1607853202273-797f1c22a38e?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Nintendo Switch OLED + 5 Game",
                        Description = "Switch OLED bản trắng, kèm dock, 2 Joy-Con neon, 5 game Mario, Zelda, Pokémon.",
                        PricePerDay = 200_000, CategoryId = gaming,
                        Location = "TP. HCM - Quận 10", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1578303512597-81e6cc155b3e?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Xbox Series X + Game Pass Ultimate",
                        Description = "Xbox Series X 1TB, 2 tay cầm không dây, 3 tháng Game Pass Ultimate. Hơn 400 game miễn phí.",
                        PricePerDay = 300_000, CategoryId = gaming,
                        Location = "Đà Nẵng - Hải Châu", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1621259182978-fbf93132d53d?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Steam Deck OLED 512GB",
                        Description = "Handheld PC gaming, OLED 7.4 inch HDR, pin 12h. Chạy được game AAA PC trên tay.",
                        PricePerDay = 250_000, CategoryId = gaming,
                        Location = "Hà Nội - Nam Từ Liêm", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1606144042614-b2417e99c4e3?w=600&q=80",
                        OwnerId = adminUser.Id
                    },

                    // === PROJECTOR (3 sản phẩm) ===
                    new() {
                        Name = "Epson EF-21 Smart 3LCD Projector",
                        Description = "Android TV tích hợp, Full HD 1080p, 2800 lumen, loa 10W. Kèm màn chiếu 100 inch.",
                        PricePerDay = 300_000, CategoryId = projector,
                        Location = "Hà Nội - Hai Bà Trưng", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "BenQ TK850i 4K HDR",
                        Description = "Máy chiếu 4K HDR-PRO, 3000 lumen, chuyên dành cho phim và thể thao. Android TV tích hợp.",
                        PricePerDay = 500_000, CategoryId = projector,
                        Location = "TP. HCM - Quận 2", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1626379953822-baec19c3accd?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "XGIMI Horizon Ultra 4K",
                        Description = "Dolby Vision, 2300 lumen, laser + LED hybrid. Thiết kế nhỏ gọn, chiếu từ 60-200 inch.",
                        PricePerDay = 450_000, CategoryId = projector,
                        Location = "Đà Nẵng - Ngũ Hành Sơn", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1478720568477-152d9b164e26?w=600&q=80",
                        OwnerId = adminUser.Id
                    },

                    // === TABLET (3 sản phẩm) ===
                    new() {
                        Name = "iPad Pro 13\" M4 + Apple Pencil Pro",
                        Description = "Màn OLED 2.7K ProMotion, chip M4, 256GB, WiFi 6E. Kèm Smart Folio Case, Apple Pencil Pro.",
                        PricePerDay = 350_000, CategoryId = tablet,
                        Location = "Đà Nẵng - Thanh Khê", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Samsung Galaxy Tab S9 Ultra",
                        Description = "Màn AMOLED 14.6 inch 120Hz, Snapdragon 8 Gen 2, 512GB, DeX mode. Kèm S Pen và Keyboard.",
                        PricePerDay = 280_000, CategoryId = tablet,
                        Location = "TP. HCM - Quận 7", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1632383691848-adb8c70d9c30?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "iPad Air M2 + Magic Keyboard",
                        Description = "Chip M2, màn Liquid Retina 11 inch, 256GB. Kèm Magic Keyboard và Apple Pencil USB-C.",
                        PricePerDay = 220_000, CategoryId = tablet,
                        Location = "Hà Nội - Hoàng Mai", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?w=600&q=80",
                        OwnerId = adminUser.Id
                    },

                    // === PHONE (3 sản phẩm) ===
                    new() {
                        Name = "iPhone 16 Pro Max 256GB",
                        Description = "Chip A18 Pro, camera 48MP ProRes 4K120fps, màn hình Super Retina XDR 6.9 inch.",
                        PricePerDay = 200_000, CategoryId = phone,
                        Location = "Hà Nội - Nam Từ Liêm", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1726878074875-e2bd2c2ef0f7?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Samsung Galaxy S24 Ultra 512GB",
                        Description = "S Pen tích hợp, camera zoom 100x, Snapdragon 8 Gen 3. Lý tưởng cho content creator.",
                        PricePerDay = 180_000, CategoryId = phone,
                        Location = "TP. HCM - Gò Vấp", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1706440622665-c8b21f7b7e3f?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Google Pixel 9 Pro 256GB",
                        Description = "AI photography, Magic Eraser, camera Tensor G4. Trải nghiệm Android gốc mượt mà nhất.",
                        PricePerDay = 160_000, CategoryId = phone,
                        Location = "Đà Nẵng - Hải Châu", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=600&q=80",
                        OwnerId = adminUser.Id
                    },

                    // === NETWORK (3 sản phẩm) ===
                    new() {
                        Name = "Starlink Standard Kit",
                        Description = "Internet vệ tinh tốc độ 200Mbps, không giới hạn data, phủ sóng toàn quốc.",
                        PricePerDay = 500_000, CategoryId = network,
                        Location = "Hà Nội - Hoàng Mai", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "TP-Link Deco XE75 Mesh WiFi 6E",
                        Description = "Hệ thống Mesh 3 nút, phủ sóng 500m², WiFi 6E tri-band, tốc độ 5400Mbps.",
                        PricePerDay = 200_000, CategoryId = network,
                        Location = "Đà Nẵng - Ngũ Hành Sơn", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                    new() {
                        Name = "Mobile WiFi 4G LTE Huawei",
                        Description = "Bộ phát WiFi di động 4G LTE, pin 5000mAh, kết nối 16 thiết bị. Kèm SIM data 100GB.",
                        PricePerDay = 80_000, CategoryId = network,
                        Location = "Hà Nội - Cầu Giấy", IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85f82e?w=600&q=80",
                        OwnerId = adminUser.Id
                    },
                };
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            await RemoveGeneratedSeedProductsAsync(context);
            await BackfillProductCoordinatesAsync(context);

            // ============================================================
            // 5. SEED BOOKINGS (dữ liệu mẫu mở rộng - 12 đơn)
            // ============================================================
            if (!await context.Bookings.AnyAsync())
            {
                var products = await context.Products.Take(12).ToListAsync();
                var renter1 = createdUsers.Count > 1 ? createdUsers[1] : adminUser;
                var renter2 = createdUsers.Count > 2 ? createdUsers[2] : adminUser;
                var renter3 = createdUsers.Count > 3 ? createdUsers[3] : adminUser;
                var renter4 = createdUsers.Count > 4 ? createdUsers[4] : adminUser;
                var renter5 = createdUsers.Count > 5 ? createdUsers[5] : adminUser;
                var renter6 = createdUsers.Count > 6 ? createdUsers[6] : adminUser;

                var bookings = new List<Booking>();
                if (products.Count >= 10)
                {
                    bookings.AddRange(new[]
                    {
                        // Đã hoàn thành
                        new Booking {
                            ProductId = products[0].Id, RenterId = renter1.Id,
                            BookingDate = DateTime.Now.AddDays(-30),
                            StartDate = DateTime.Now.AddDays(-25), EndDate = DateTime.Now.AddDays(-22),
                            TotalAmount = products[0].PricePerDay * 3,
                            Status = BookingStatus.Completed
                        },
                        new Booking {
                            ProductId = products[1].Id, RenterId = renter2.Id,
                            BookingDate = DateTime.Now.AddDays(-25),
                            StartDate = DateTime.Now.AddDays(-20), EndDate = DateTime.Now.AddDays(-17),
                            TotalAmount = products[1].PricePerDay * 3,
                            Status = BookingStatus.Completed
                        },
                        new Booking {
                            ProductId = products[6].Id, RenterId = renter3.Id,
                            BookingDate = DateTime.Now.AddDays(-20),
                            StartDate = DateTime.Now.AddDays(-18), EndDate = DateTime.Now.AddDays(-15),
                            TotalAmount = products[6].PricePerDay * 3,
                            Status = BookingStatus.Completed
                        },
                        new Booking {
                            ProductId = products[3].Id, RenterId = renter4.Id,
                            BookingDate = DateTime.Now.AddDays(-18),
                            StartDate = DateTime.Now.AddDays(-15), EndDate = DateTime.Now.AddDays(-12),
                            TotalAmount = products[3].PricePerDay * 3,
                            Status = BookingStatus.Completed
                        },
                        new Booking {
                            ProductId = products[8].Id, RenterId = renter5.Id,
                            BookingDate = DateTime.Now.AddDays(-15),
                            StartDate = DateTime.Now.AddDays(-12), EndDate = DateTime.Now.AddDays(-10),
                            TotalAmount = products[8].PricePerDay * 2,
                            Status = BookingStatus.Completed
                        },

                        // Đã duyệt (đang thuê)
                        new Booking {
                            ProductId = products[2].Id, RenterId = renter1.Id,
                            BookingDate = DateTime.Now.AddDays(-5),
                            StartDate = DateTime.Now.AddDays(-3), EndDate = DateTime.Now.AddDays(2),
                            TotalAmount = products[2].PricePerDay * 5,
                            Status = BookingStatus.Approved
                        },
                        new Booking {
                            ProductId = products[5].Id, RenterId = renter6.Id,
                            BookingDate = DateTime.Now.AddDays(-3),
                            StartDate = DateTime.Now.AddDays(-1), EndDate = DateTime.Now.AddDays(4),
                            TotalAmount = products[5].PricePerDay * 5,
                            Status = BookingStatus.Approved
                        },

                        // Đang chờ duyệt
                        new Booking {
                            ProductId = products[4].Id, RenterId = renter3.Id,
                            BookingDate = DateTime.Now.AddDays(-1),
                            StartDate = DateTime.Now.AddDays(2), EndDate = DateTime.Now.AddDays(5),
                            TotalAmount = products[4].PricePerDay * 3,
                            Status = BookingStatus.Pending
                        },
                        new Booking {
                            ProductId = products[7].Id, RenterId = renter2.Id,
                            BookingDate = DateTime.Now,
                            StartDate = DateTime.Now.AddDays(3), EndDate = DateTime.Now.AddDays(7),
                            TotalAmount = products[7].PricePerDay * 4,
                            Status = BookingStatus.Pending
                        },
                        new Booking {
                            ProductId = products[9].Id, RenterId = renter4.Id,
                            BookingDate = DateTime.Now,
                            StartDate = DateTime.Now.AddDays(1), EndDate = DateTime.Now.AddDays(3),
                            TotalAmount = products[9].PricePerDay * 2,
                            Status = BookingStatus.Pending
                        },

                        // Đã hủy
                        new Booking {
                            ProductId = products[0].Id, RenterId = renter5.Id,
                            BookingDate = DateTime.Now.AddDays(-10),
                            StartDate = DateTime.Now.AddDays(-8), EndDate = DateTime.Now.AddDays(-5),
                            TotalAmount = products[0].PricePerDay * 3,
                            Status = BookingStatus.Cancelled
                        },
                        new Booking {
                            ProductId = products[1].Id, RenterId = renter6.Id,
                            BookingDate = DateTime.Now.AddDays(-7),
                            StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(-2),
                            TotalAmount = products[1].PricePerDay * 3,
                            Status = BookingStatus.Cancelled
                        },
                    });
                    context.Bookings.AddRange(bookings);
                    await context.SaveChangesAsync();
                }

                // ============================================================
                // 6. SEED REVIEWS (phong phú hơn - 8 đánh giá)
                // ============================================================
                if (!await context.Reviews.AnyAsync())
                {
                    var savedBookings = await context.Bookings
                        .Where(b => b.Status == BookingStatus.Completed)
                        .ToListAsync();

                    var reviewComments = new[]
                    {
                        "Thiết bị như mô tả, giao hàng đúng hẹn. Rất hài lòng!",
                        "Chất lượng tuyệt vời, sẽ thuê lại lần sau.",
                        "Máy chạy mượt mà, pin tốt. Giá cả hợp lý.",
                        "Trải nghiệm tuyệt vời! Chủ hàng nhiệt tình và hỗ trợ tận tình.",
                        "Thiết bị còn mới, đóng gói cẩn thận. 5 sao!",
                        "Dùng cho dự án rất ổn, tiết kiệm chi phí mua mới.",
                        "Giao hàng nhanh, thiết bị sạch sẽ và hoạt động tốt.",
                        "Đã thuê nhiều lần, lần nào cũng hài lòng. Highly recommend!"
                    };

                    var reviews = new List<Review>();
                    foreach (var b in savedBookings)
                    {
                        reviews.Add(new Review
                        {
                            ProductId = b.ProductId,
                            ReviewerId = b.RenterId,
                            Rating = new[] { 4, 5, 5, 4, 5, 5, 4, 5 }[reviews.Count % 8],
                            Comment = reviewComments[reviews.Count % reviewComments.Length],
                            CreatedAt = b.EndDate.AddDays(1)
                        });
                    }
                    context.Reviews.AddRange(reviews);
                    await context.SaveChangesAsync();
                }
            }

            // ============================================================
            // 7. SEED CONTACT MESSAGES (dữ liệu mẫu)
            // ============================================================
            if (!await context.ContactMessages.AnyAsync())
            {
                var contacts = new List<ContactMessage>
                {
                    new() {
                        FullName = "Nguyễn Văn A",
                        Email = "nguyen.van.a@demo.com",
                        Phone = "0912345678",
                        Subject = "Hỏi về sản phẩm",
                        Message = "Cho tôi hỏi MacBook Pro 16 M3 Max hiện còn sẵn cho thuê không ạ? Tôi muốn thuê 1 tuần cho dự án thiết kế.",
                        CreatedAt = DateTime.Now.AddDays(-5),
                        Status = ContactStatus.Replied,
                        SenderId = createdUsers.Count > 1 ? createdUsers[1].Id : null,
                        AdminReply = "Chào bạn! MacBook Pro 16 M3 Max hiện vẫn còn sẵn. Bạn có thể đặt thuê trực tiếp trên website nhé. Cảm ơn bạn!",
                        RepliedAt = DateTime.Now.AddDays(-4)
                    },
                    new() {
                        FullName = "Trần Thị B",
                        Email = "tran.thi.b@demo.com",
                        Phone = "0923456789",
                        Subject = "Vấn đề đặt hàng",
                        Message = "Tôi đã đặt thuê Sony A7R V nhưng chưa nhận được xác nhận. Đơn hàng của tôi có vấn đề gì không ạ?",
                        CreatedAt = DateTime.Now.AddDays(-3),
                        Status = ContactStatus.Read,
                        SenderId = createdUsers.Count > 2 ? createdUsers[2].Id : null
                    },
                    new() {
                        FullName = "Phạm Thị D",
                        Email = "pham.thi.d@demo.com",
                        Phone = "0945678901",
                        Subject = "Góp ý dịch vụ",
                        Message = "Website rất đẹp và dễ sử dụng! Tôi muốn đề xuất thêm chức năng so sánh sản phẩm để dễ chọn hơn ạ.",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        Status = ContactStatus.New,
                        SenderId = createdUsers.Count > 4 ? createdUsers[4].Id : null
                    },
                    new() {
                        FullName = "Khách vãng lai",
                        Email = "guest@email.com",
                        Phone = "0967890123",
                        Subject = "Hợp tác kinh doanh",
                        Message = "Tôi là chủ cửa hàng thiết bị công nghệ tại Hà Nội. Muốn hợp tác cho thuê thiết bị qua TechShare. Vui lòng liên hệ lại.",
                        CreatedAt = DateTime.Now,
                        Status = ContactStatus.New
                    },
                };
                context.ContactMessages.AddRange(contacts);
                await context.SaveChangesAsync();
            }

            // ============================================================
            // 8. SEED WISHLISTS (dữ liệu mẫu)
            // ============================================================
            if (!await context.Wishlists.AnyAsync())
            {
                var allProducts = await context.Products.Take(8).ToListAsync();
                var wishlists = new List<Wishlist>();

                if (createdUsers.Count > 1 && allProducts.Count > 3)
                {
                    wishlists.Add(new Wishlist { UserId = createdUsers[1].Id, ProductId = allProducts[0].Id, AddedAt = DateTime.Now.AddDays(-5) });
                    wishlists.Add(new Wishlist { UserId = createdUsers[1].Id, ProductId = allProducts[3].Id, AddedAt = DateTime.Now.AddDays(-3) });
                    wishlists.Add(new Wishlist { UserId = createdUsers[1].Id, ProductId = allProducts[6].Id, AddedAt = DateTime.Now.AddDays(-1) });
                }
                if (createdUsers.Count > 2 && allProducts.Count > 5)
                {
                    wishlists.Add(new Wishlist { UserId = createdUsers[2].Id, ProductId = allProducts[1].Id, AddedAt = DateTime.Now.AddDays(-4) });
                    wishlists.Add(new Wishlist { UserId = createdUsers[2].Id, ProductId = allProducts[5].Id, AddedAt = DateTime.Now.AddDays(-2) });
                }
                if (createdUsers.Count > 3 && allProducts.Count > 7)
                {
                    wishlists.Add(new Wishlist { UserId = createdUsers[3].Id, ProductId = allProducts[2].Id, AddedAt = DateTime.Now.AddDays(-3) });
                    wishlists.Add(new Wishlist { UserId = createdUsers[3].Id, ProductId = allProducts[7].Id, AddedAt = DateTime.Now.AddDays(-1) });
                }

                if (wishlists.Any())
                {
                    context.Wishlists.AddRange(wishlists);
                    await context.SaveChangesAsync();
                }
            }

            // ============================================================
            // 9. SEED MARKETPLACE LISTINGS (dữ liệu mẫu)
            // ============================================================
            if (!await context.MarketplaceListings.AnyAsync())
            {
                var cats = await context.Categories.ToListAsync();
                if (cats.Any() && createdUsers.Count > 1)
                {
                    var listings = new List<MarketplaceListing>
                    {
                        new MarketplaceListing
                        {
                            Title = "iPhone 13 Pro Max 256GB - VN/A",
                            Description = "Máy zin nguyên bản, pin 89%, ngoại hình 98% có xước nhẹ viền. Kèm cáp sạc và ốp lưng UAG. Pass lại để lên đời.",
                            Price = 16_500_000,
                            OriginalPrice = 28_000_000,
                            Condition = ProductCondition.Good,
                            ImageUrl = "https://images.unsplash.com/photo-1632661674596-df8be070a5c5?w=600&q=80",
                            Location = "Hà Nội - Đống Đa",
                            SellerId = createdUsers[1].Id,
                            CategoryId = cats.First(c => c.Name.Contains("Điện thoại")).Id,
                            CreatedAt = DateTime.Now.AddDays(-2)
                        },
                        new MarketplaceListing
                        {
                            Title = "Bàn phím cơ Keychron K8 Pro",
                            Description = "Dùng chưa tới 1 tháng, switch Brown, LED RGB, khung nhôm. Do ồn quá mang lên công ty bị mắng nên bán. Fullbox như mới.",
                            Price = 1_800_000,
                            OriginalPrice = 2_400_000,
                            Condition = ProductCondition.LikeNew,
                            ImageUrl = "https://images.unsplash.com/photo-1595225476474-87563907a212?w=600&q=80",
                            Location = "TP. HCM - Quận 3",
                            SellerId = createdUsers.Count > 2 ? createdUsers[2].Id : adminUser.Id,
                            CategoryId = cats.First(c => c.Name.Contains("Laptop")).Id, // Phụ kiện máy tính
                            CreatedAt = DateTime.Now.AddDays(-5)
                        },
                        new MarketplaceListing
                        {
                            Title = "Máy ảnh Sony A6000 + kit 16-50",
                            Description = "Máy chuyên cất tủ chống ẩm, thỉnh thoảng mang đi du lịch. Body hơi cũ theo thời gian, trầy màn hình một góc nhưng không ảnh hưởng hiển thị. Nhận fix nhẹ cho anh em nhiệt tình.",
                            Price = 6_500_000,
                            Condition = ProductCondition.Fair,
                            ImageUrl = "https://images.unsplash.com/photo-1620021665476-cff9a9bded28?w=600&q=80",
                            Location = "Đà Nẵng - Hải Châu",
                            SellerId = createdUsers.Count > 3 ? createdUsers[3].Id : adminUser.Id,
                            CategoryId = cats.First(c => c.Name.Contains("Máy ảnh")).Id,
                            CreatedAt = DateTime.Now.AddDays(-1)
                        }
                    };
                    context.MarketplaceListings.AddRange(listings);
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task BackfillProductCoordinatesAsync(ApplicationDbContext context)
        {
            var products = await context.Products.ToListAsync();
            if (!products.Any()) return;

            var changed = false;
            foreach (var product in products)
            {
                var hasValidCoords = product.Latitude.HasValue && product.Longitude.HasValue &&
                                     product.Latitude.Value >= -90 && product.Latitude.Value <= 90 &&
                                     product.Longitude.Value >= -180 && product.Longitude.Value <= 180;

                if (hasValidCoords) continue;

                if (TryResolveCoordinatesFromLocation(product.Location, out var lat, out var lng))
                {
                    product.Latitude = lat;
                    product.Longitude = lng;
                    changed = true;
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync();
            }
        }

        private static async Task RemoveGeneratedSeedProductsAsync(ApplicationDbContext context)
        {
            var generatedProducts = await context.Products
                .Where(p => p.Description != null &&
                            (EF.Functions.Like(p.Description, "%Mã mẫu #%") ||
                             EF.Functions.Like(p.Description, "%Ma mau #%")))
                .ToListAsync();

            if (!generatedProducts.Any())
            {
                return;
            }

            context.Products.RemoveRange(generatedProducts);
            await context.SaveChangesAsync();
        }

        private static bool TryResolveCoordinatesFromLocation(string? location, out double lat, out double lng)
        {
            lat = 0;
            lng = 0;
            if (string.IsNullOrWhiteSpace(location)) return false;

            var key = NormalizeLocationKey(location);

            var locationMap = new (string Key, double Lat, double Lng)[]
            {
                ("ha noi - cau giay", 21.0366, 105.7906),
                ("ha noi - dong da", 21.0180, 105.8293),
                ("ha noi - hoan kiem", 21.0288, 105.8522),
                ("ha noi - tay ho", 21.0700, 105.8180),
                ("ha noi - hoang mai", 20.9741, 105.8588),
                ("ha noi - nam tu liem", 21.0032, 105.7707),
                ("ha noi - hai ba trung", 21.0058, 105.8576),
                ("tp. hcm - quan 1", 10.7757, 106.7004),
                ("tp. hcm - quan 2", 10.7872, 106.7498),
                ("tp. hcm - quan 3", 10.7866, 106.6823),
                ("tp. hcm - quan 7", 10.7343, 106.7218),
                ("tp. hcm - quan 10", 10.7726, 106.6677),
                ("tp. hcm - go vap", 10.8400, 106.6650),
                ("tp. hcm - binh thanh", 10.8085, 106.7091),
                ("da nang - hai chau", 16.0471, 108.2197),
                ("da nang - son tra", 16.0756, 108.2397),
                ("da nang - thanh khe", 16.0715, 108.1956),
                ("da nang - ngu hanh son", 16.0389, 108.2520),
                ("nha trang - tran phu", 12.2388, 109.1967),
                ("thu duc", 10.8537, 106.7887)
            };

            foreach (var entry in locationMap)
            {
                if (key.Contains(entry.Key))
                {
                    lat = entry.Lat;
                    lng = entry.Lng;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeLocationKey(string input)
        {
            var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c == 'đ' ? 'd' : c);
                }
            }

            return sb.ToString();
        }

        private static async Task EnsureProductsPerCategoryAsync(
            ApplicationDbContext context,
            string ownerId,
            string webRootPath)
        {
            const int targetPerCategory = 20;

            var categories = await context.Categories.OrderBy(c => c.Id).ToListAsync();
            if (!categories.Any())
            {
                return;
            }

            var categoryDefinitions = GetCategoryDefinitions();
            var categoryMap = ResolveCategoryMap(categories, categoryDefinitions);
            var existingProducts = await context.Products.ToListAsync();
            var existingNames = new HashSet<string>(existingProducts.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var productsToAdd = new List<Product>();

            foreach (var definition in categoryDefinitions)
            {
                if (!categoryMap.TryGetValue(definition.Key, out var category))
                {
                    continue;
                }

                var existingInCategory = existingProducts.Count(p => p.CategoryId == category.Id);
                if (existingInCategory >= targetPerCategory)
                {
                    continue;
                }

                var needed = targetPerCategory - existingInCategory;
                for (var i = 0; i < needed; i++)
                {
                    var sequence = existingInCategory + i + 1;
                    var model = definition.Models[(sequence - 1) % definition.Models.Length];
                    var edition = definition.Editions[((sequence - 1) / definition.Models.Length) % definition.Editions.Length];
                    var baseName = $"{model} {edition}";
                    var productName = EnsureUniqueName(baseName, existingNames);
                    var location = definition.Locations[(sequence - 1) % definition.Locations.Length];
                    var price = definition.BasePrice + (((sequence - 1) % 6) * definition.PriceStep);
                    var imageUrl = EnsureProductImage(
                        webRootPath,
                        definition.Key,
                        productName,
                        sequence,
                        forceRefresh: string.Equals(definition.Key, "phone", StringComparison.OrdinalIgnoreCase));

                    productsToAdd.Add(new Product
                    {
                        Name = productName,
                        Description = BuildProductDescription(definition.DisplayName, model, edition, sequence),
                        PricePerDay = price,
                        CategoryId = category.Id,
                        Location = location,
                        IsAvailable = true,
                        ImageUrl = imageUrl,
                        OwnerId = ownerId
                    });
                }
            }

            if (productsToAdd.Any())
            {
                context.Products.AddRange(productsToAdd);
                await context.SaveChangesAsync();
            }

            await EnsureLocalImagesForExistingProductsAsync(context, categories, webRootPath, categoryDefinitions);
        }

        private static async Task EnsureLocalImagesForExistingProductsAsync(
            ApplicationDbContext context,
            IReadOnlyCollection<Category> categories,
            string webRootPath,
            IReadOnlyList<CategorySeedDefinition> definitions)
        {
            var categoryById = categories.ToDictionary(c => c.Id, c => c);
            var fallbackKey = definitions.First().Key;
            var changed = false;
            var pendingSaves = 0;

            var products = await context.Products.ToListAsync();
            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];

                var categoryKey = fallbackKey;
                if (categoryById.TryGetValue(product.CategoryId, out var cat))
                {
                    categoryKey = ResolveCategoryKey(cat.Name, definitions) ?? fallbackKey;
                }

                var shouldRefresh = ShouldRefreshSeedImage(product.ImageUrl, categoryKey);
                if (!shouldRefresh &&
                    !string.IsNullOrWhiteSpace(product.ImageUrl) &&
                    product.ImageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) &&
                    !product.ImageUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) &&
                    TryResolveLocalImagePath(webRootPath, product.ImageUrl, out _))
                {
                    continue;
                }

                product.ImageUrl = EnsureProductImage(
                    webRootPath,
                    categoryKey,
                    product.Name,
                    product.Id > 0 ? product.Id : (i + 1),
                    forceRefresh: shouldRefresh);
                changed = true;
                pendingSaves++;

                if (pendingSaves >= 20)
                {
                    await context.SaveChangesAsync();
                    pendingSaves = 0;
                }
            }

            if (changed && pendingSaves > 0)
            {
                await context.SaveChangesAsync();
            }
        }

        private static bool ShouldRefreshSeedImage(string? imageUrl, string? categoryKey)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return true;
            }

            if (imageUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(categoryKey, "phone", StringComparison.OrdinalIgnoreCase) &&
                imageUrl.StartsWith("/images/seed-products/phone/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static Dictionary<string, Category> ResolveCategoryMap(
            IReadOnlyList<Category> categories,
            IReadOnlyList<CategorySeedDefinition> definitions)
        {
            var result = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
            var usedCategoryIds = new HashSet<int>();

            foreach (var definition in definitions)
            {
                var category = categories.FirstOrDefault(c =>
                {
                    var key = NormalizeText(c.Name);
                    return key.Contains(definition.MatchKeyword, StringComparison.OrdinalIgnoreCase);
                });

                if (category != null && usedCategoryIds.Add(category.Id))
                {
                    result[definition.Key] = category;
                }
            }

            for (var i = 0; i < definitions.Count && i < categories.Count; i++)
            {
                var key = definitions[i].Key;
                if (result.ContainsKey(key))
                {
                    continue;
                }

                var fallback = categories.FirstOrDefault(c => !usedCategoryIds.Contains(c.Id));
                if (fallback == null)
                {
                    continue;
                }

                usedCategoryIds.Add(fallback.Id);
                result[key] = fallback;
            }

            return result;
        }

        private static string? ResolveCategoryKey(string categoryName, IReadOnlyList<CategorySeedDefinition> definitions)
        {
            var normalized = NormalizeText(categoryName);
            return definitions
                .FirstOrDefault(d => normalized.Contains(d.MatchKeyword, StringComparison.OrdinalIgnoreCase))
                ?.Key;
        }

        private static string NormalizeText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c == 'đ' ? 'd' : c);
                }
            }

            return sb.ToString();
        }

        private static string EnsureUniqueName(string baseName, ISet<string> existingNames)
        {
            if (!existingNames.Contains(baseName))
            {
                existingNames.Add(baseName);
                return baseName;
            }

            var counter = 2;
            var candidate = $"{baseName} - {counter}";
            while (existingNames.Contains(candidate))
            {
                counter++;
                candidate = $"{baseName} - {counter}";
            }

            existingNames.Add(candidate);
            return candidate;
        }

        private static string BuildProductDescription(string categoryDisplayName, string model, string edition, int sequence)
        {
            return $"{model} phiên bản {edition}, thuộc nhóm {categoryDisplayName}. " +
                   $"Thiết bị hoạt động ổn định, đã kiểm tra kỹ trước khi cho thuê. " +
                   $"Mã mẫu #{sequence:00}, phù hợp cho công việc và nhu cầu thực tế.";
        }

        private static string EnsureProductImage(
            string webRootPath,
            string categoryKey,
            string productName,
            int sequence,
            bool forceRefresh = false)
        {
            var safeKey = Slugify(categoryKey);
            var safeName = Slugify(productName);
            var relativeFolder = Path.Combine("images", "seed-products", safeKey);
            var absoluteFolder = Path.Combine(webRootPath, relativeFolder);
            Directory.CreateDirectory(absoluteFolder);

            var fileBaseName = $"{sequence:00}-{safeName}";
            var absolutePath = Path.Combine(absoluteFolder, $"{fileBaseName}.jpg");
            if (forceRefresh)
            {
                DeleteExistingSeedImageFiles(absoluteFolder, fileBaseName);
            }

            if (!forceRefresh && TryGetExistingSeedImageFile(absoluteFolder, fileBaseName, out var existingFileName))
            {
                return $"/images/seed-products/{safeKey}/{existingFileName}".Replace("\\", "/");
            }

            if (!File.Exists(absolutePath))
            {
                if (!TryDownloadProductSpecificPhoto(categoryKey, productName, absoluteFolder, fileBaseName, out var downloadedFileName) &&
                    !TryDownloadStockPhoto(categoryKey, sequence, absoluteFolder, fileBaseName, out downloadedFileName))
                {
                    if (!TryCopyFallbackPhoto(webRootPath, absolutePath))
                    {
                        var fallbackSvg = Path.Combine(absoluteFolder, $"{fileBaseName}.svg");
                        var svg = BuildProductSvg(categoryKey, productName);
                        File.WriteAllText(fallbackSvg, svg, new UTF8Encoding(false));
                        return $"/images/seed-products/{safeKey}/{fileBaseName}.svg";
                    }
                }
                else
                {
                    return $"/images/seed-products/{safeKey}/{downloadedFileName}".Replace("\\", "/");
                }
            }

            var webPath = $"/images/seed-products/{safeKey}/{fileBaseName}.jpg";
            return webPath.Replace("\\", "/");
        }

        private static bool TryDownloadProductSpecificPhoto(
            string categoryKey,
            string productName,
            string destinationFolder,
            string fileBaseName,
            out string savedFileName)
        {
            savedFileName = string.Empty;
            var searchName = BuildProductImageSearchName(productName);

            if (string.Equals(categoryKey, "phone", StringComparison.OrdinalIgnoreCase) &&
                TryDownloadFromGsmArena(searchName, destinationFolder, fileBaseName, out savedFileName))
            {
                return true;
            }

            var primaryQuery = $"{searchName} {GetCategorySearchTags(categoryKey)}";
            return TryDownloadFromBingImageSearch(primaryQuery, destinationFolder, fileBaseName, out savedFileName) ||
                   TryDownloadFromBingImageSearch(searchName, destinationFolder, fileBaseName, out savedFileName);
        }

        private static bool TryDownloadStockPhoto(
            string categoryKey,
            int sequence,
            string destinationFolder,
            string fileBaseName,
            out string savedFileName)
        {
            savedFileName = string.Empty;
            var lockId = Math.Abs(sequence) + 1000;
            var urlCandidates = new[]
            {
                $"https://picsum.photos/seed/{Slugify(categoryKey)}-{lockId}/1200/800.jpg"
            };

            foreach (var url in urlCandidates)
            {
                if (TryDownloadImageFromUrl(url, destinationFolder, fileBaseName, out savedFileName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryDownloadFromGsmArena(
            string productName,
            string destinationFolder,
            string fileBaseName,
            out string savedFileName)
        {
            savedFileName = string.Empty;
            try
            {
                var searchUrl = $"https://www.gsmarena.com/res.php3?sSearch={Uri.EscapeDataString(productName)}";
                var html = SeedImageHttpClient.GetStringAsync(searchUrl).GetAwaiter().GetResult();
                var makersIndex = html.IndexOf("class=\"makers\"", StringComparison.OrdinalIgnoreCase);
                if (makersIndex < 0)
                {
                    return false;
                }

                var scanArea = html.Substring(makersIndex, Math.Min(80_000, html.Length - makersIndex));
                var match = Regex.Match(scanArea, "<img\\s+src=\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    return false;
                }

                var imageUrl = match.Groups["url"].Value.Trim();
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return false;
                }

                if (imageUrl.StartsWith("//", StringComparison.Ordinal))
                {
                    imageUrl = "https:" + imageUrl;
                }
                else if (imageUrl.StartsWith("/", StringComparison.Ordinal))
                {
                    imageUrl = "https://www.gsmarena.com" + imageUrl;
                }

                return TryDownloadImageFromUrl(imageUrl, destinationFolder, fileBaseName, out savedFileName, minimumBytes: 4_000);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryDownloadFromBingImageSearch(
            string productName,
            string destinationFolder,
            string fileBaseName,
            out string savedFileName)
        {
            savedFileName = string.Empty;
            try
            {
                var query = Uri.EscapeDataString($"{productName} official image");
                var searchUrl = $"https://www.bing.com/images/search?q={query}";
                var html = SeedImageHttpClient.GetStringAsync(searchUrl).GetAwaiter().GetResult();

                var matches = Regex.Matches(html, "murl&quot;:&quot;(?<url>[^&]*?)&quot;", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var rawUrl = WebUtility.HtmlDecode(match.Groups["url"].Value);
                    if (string.IsNullOrWhiteSpace(rawUrl))
                    {
                        continue;
                    }

                    if (!IsSupportedSeedImageUrl(rawUrl))
                    {
                        continue;
                    }

                    if (TryDownloadImageFromUrl(rawUrl, destinationFolder, fileBaseName, out savedFileName, minimumBytes: 4_000))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore and fallback to other providers
            }

            return false;
        }

        private static bool IsSupportedSeedImageUrl(string rawUrl)
        {
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var ext = Path.GetExtension(uri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(ext))
            {
                return true;
            }

            return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryDownloadImageFromUrl(
            string url,
            string destinationFolder,
            string fileBaseName,
            out string savedFileName,
            int minimumBytes = 10_000)
        {
            savedFileName = string.Empty;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("TechShareSeed/1.0");
                using var response = SeedImageHttpClient.Send(request);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (string.IsNullOrWhiteSpace(mediaType) ||
                    (!mediaType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) &&
                     !mediaType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) &&
                     !mediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase) &&
                     !mediaType.Equals("image/webp", StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                if (bytes.Length < minimumBytes)
                {
                    return false;
                }

                if (!IsSupportedImageBytes(bytes))
                {
                    return false;
                }

                var extension = ResolveImageExtension(mediaType, url);
                DeleteExistingSeedImageFiles(destinationFolder, fileBaseName);
                var fileName = $"{fileBaseName}{extension}";
                var destinationPath = Path.Combine(destinationFolder, fileName);
                File.WriteAllBytes(destinationPath, bytes);
                savedFileName = fileName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSupportedImageBytes(byte[] bytes)
        {
            if (bytes.Length < 8)
            {
                return false;
            }

            var isJpeg = bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
            var isPng = bytes[0] == 0x89 &&
                        bytes[1] == 0x50 &&
                        bytes[2] == 0x4E &&
                        bytes[3] == 0x47 &&
                        bytes[4] == 0x0D &&
                        bytes[5] == 0x0A &&
                        bytes[6] == 0x1A &&
                        bytes[7] == 0x0A;
            var isWebp = bytes.Length >= 12 &&
                         bytes[0] == 0x52 &&
                         bytes[1] == 0x49 &&
                         bytes[2] == 0x46 &&
                         bytes[3] == 0x46 &&
                         bytes[8] == 0x57 &&
                         bytes[9] == 0x45 &&
                         bytes[10] == 0x42 &&
                         bytes[11] == 0x50;

            return isJpeg || isPng || isWebp;
        }

        private static string BuildProductImageSearchName(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return string.Empty;
            }

            var cleaned = productName;
            var removeTokens = new[]
            {
                "Creator Pack", "Business Pack", "Film Kit", "Travel Kit",
                "Studio Pack", "Live Pack", "Esports Pack", "Home Pack",
                "Cinema Pack", "Presentation Pack", "Creator Bundle", "Learning Bundle",
                "Office Kit", "Home Kit", "256GB", "512GB"
            };

            foreach (var token in removeTokens)
            {
                cleaned = cleaned.Replace(token, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            cleaned = Regex.Replace(cleaned, "\\s+", " ").Trim();
            return cleaned;
        }

        private static bool TryCopyFallbackPhoto(string webRootPath, string destinationPath)
        {
            var fallbackDir = Path.Combine(webRootPath, "images");
            if (!Directory.Exists(fallbackDir))
            {
                return false;
            }

            var fallbackFiles = Directory.GetFiles(fallbackDir)
                .Where(path =>
                {
                    var ext = Path.GetExtension(path);
                    return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            if (!fallbackFiles.Any())
            {
                return false;
            }

            var hash = StableHash(destinationPath);
            var selected = fallbackFiles[hash % fallbackFiles.Count];
            File.Copy(selected, destinationPath, overwrite: true);
            return true;
        }

        private static string GetCategorySearchTags(string categoryKey)
        {
            return categoryKey.ToLowerInvariant() switch
            {
                "laptop" => "laptop,computer",
                "camera" => "camera,photography",
                "audio" => "headphones,speaker",
                "gaming" => "gaming,console",
                "projector" => "projector,screen",
                "tablet" => "tablet,ipad",
                "phone" => "smartphone,mobile",
                "network" => "router,wifi",
                _ => "technology,gadget"
            };
        }

        private static bool TryResolveLocalImagePath(string webRootPath, string imageUrl, out string absolutePath)
        {
            absolutePath = string.Empty;
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var cleanUrl = imageUrl.Split('?', '#')[0];
            if (string.IsNullOrWhiteSpace(cleanUrl))
            {
                return false;
            }

            var relative = cleanUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var combined = Path.Combine(webRootPath, relative);
            if (!File.Exists(combined))
            {
                return false;
            }

            absolutePath = combined;
            return true;
        }

        private static bool TryGetExistingSeedImageFile(string folder, string fileBaseName, out string fileName)
        {
            fileName = string.Empty;
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".webp" })
            {
                var candidate = Path.Combine(folder, $"{fileBaseName}{ext}");
                if (!File.Exists(candidate))
                {
                    continue;
                }

                fileName = $"{fileBaseName}{ext}";
                return true;
            }

            return false;
        }

        private static void DeleteExistingSeedImageFiles(string folder, string fileBaseName)
        {
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" })
            {
                var path = Path.Combine(folder, $"{fileBaseName}{ext}");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static string ResolveImageExtension(string mediaType, string url)
        {
            if (mediaType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            {
                return ".webp";
            }

            if (mediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            {
                return ".png";
            }

            var ext = Path.GetExtension(url.Split('?', '#')[0]);
            if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                return ".png";
            }

            if (ext.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            {
                return ".webp";
            }

            return ".jpg";
        }

        private static string BuildProductSvg(string categoryKey, string productName)
        {
            var colors = GetGradientFromText(productName + categoryKey);
            var title = EscapeXml(productName);
            var subtitle = EscapeXml(categoryKey.ToUpperInvariant());

            return $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800">
                  <defs>
                    <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
                      <stop offset="0%" stop-color="{colors.Start}" />
                      <stop offset="100%" stop-color="{colors.End}" />
                    </linearGradient>
                  </defs>
                  <rect width="1200" height="800" fill="url(#g)" />
                  <rect x="52" y="52" width="1096" height="696" rx="24" fill="rgba(255,255,255,0.12)" stroke="rgba(255,255,255,0.35)" />
                  <text x="90" y="170" font-size="38" font-family="Segoe UI, Arial, sans-serif" fill="#ffffff" font-weight="700">{subtitle}</text>
                  <text x="90" y="250" font-size="54" font-family="Segoe UI, Arial, sans-serif" fill="#ffffff" font-weight="800">{title}</text>
                  <text x="90" y="320" font-size="30" font-family="Segoe UI, Arial, sans-serif" fill="#f8f9fa">TechShare Seed Product</text>
                </svg>
                """;
        }

        private static (string Start, string End) GetGradientFromText(string input)
        {
            var palette = new (string Start, string End)[]
            {
                ("#3b82f6", "#1d4ed8"),
                ("#22c55e", "#15803d"),
                ("#a855f7", "#6b21a8"),
                ("#f97316", "#c2410c"),
                ("#14b8a6", "#0f766e"),
                ("#ec4899", "#be185d"),
                ("#64748b", "#334155"),
                ("#0ea5e9", "#0369a1"),
            };

            var hash = StableHash(input);
            return palette[hash % palette.Length];
        }

        private static int StableHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var value = BitConverter.ToInt32(bytes, 0);
            return value == int.MinValue ? 0 : Math.Abs(value);
        }

        private static string EscapeXml(string value)
        {
            return value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&apos;", StringComparison.Ordinal);
        }

        private static string Slugify(string input)
        {
            var normalized = NormalizeText(input);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }

            var slug = sb.ToString().Trim('-');
            if (slug.Length > 48)
            {
                slug = slug[..48].Trim('-');
            }

            return string.IsNullOrWhiteSpace(slug) ? "item" : slug;
        }

        private static List<CategorySeedDefinition> GetCategoryDefinitions()
        {
            return new List<CategorySeedDefinition>
            {
                new(
                    Key: "laptop",
                    DisplayName: "Laptop & May tinh",
                    MatchKeyword: "laptop",
                    BasePrice: 220_000,
                    PriceStep: 30_000,
                    Locations: new[] { "Ha Noi - Cau Giay", "TP. HCM - Quan 1", "Da Nang - Hai Chau", "Ha Noi - Nam Tu Liem" },
                    Models: new[] { "MacBook Pro 14", "MacBook Air 15", "Dell XPS 13", "Dell XPS 15", "ThinkPad X1 Carbon", "ASUS Zenbook 14", "HP Spectre x360", "MSI Prestige 16", "Acer Swift Edge", "Surface Laptop 6" },
                    Editions: new[] { "Creator Pack", "Business Pack" }),

                new(
                    Key: "camera",
                    DisplayName: "May anh & Quay phim",
                    MatchKeyword: "may anh",
                    BasePrice: 240_000,
                    PriceStep: 35_000,
                    Locations: new[] { "Ha Noi - Hoan Kiem", "TP. HCM - Quan 3", "Da Nang - Son Tra", "Nha Trang - Tran Phu" },
                    Models: new[] { "Sony A7 IV", "Sony FX30", "Canon R6 Mark II", "Canon R8", "Fujifilm X-T5", "Nikon Z6 II", "Panasonic S5 II", "DJI Pocket 3", "GoPro Hero 12", "DJI Mavic 3" },
                    Editions: new[] { "Film Kit", "Travel Kit" }),

                new(
                    Key: "audio",
                    DisplayName: "Am thanh & Loa",
                    MatchKeyword: "am thanh",
                    BasePrice: 120_000,
                    PriceStep: 20_000,
                    Locations: new[] { "Ha Noi - Dong Da", "TP. HCM - Binh Thanh", "Da Nang - Thanh Khe", "Ha Noi - Cau Giay" },
                    Models: new[] { "JBL PartyBox 310", "Bose QC Ultra", "Sony WH-1000XM5", "Marshall Stanmore III", "Yamaha HS8 Pair", "Shure SM7B Set", "Rode NT1 Kit", "Audio Technica M50x", "Sennheiser EW-D", "Focusrite Scarlett 2i2" },
                    Editions: new[] { "Studio Pack", "Live Pack" }),

                new(
                    Key: "gaming",
                    DisplayName: "Console & Gaming",
                    MatchKeyword: "console",
                    BasePrice: 140_000,
                    PriceStep: 25_000,
                    Locations: new[] { "Ha Noi - Cau Giay", "TP. HCM - Quan 10", "Da Nang - Hai Chau", "Ha Noi - Hoang Mai" },
                    Models: new[] { "PlayStation 5 Slim", "Xbox Series X", "Nintendo Switch OLED", "Steam Deck OLED", "ROG Ally X", "Logitech G Pro Rig", "Thrustmaster T300 RS", "Meta Quest 3", "Razer Blade 16", "Corsair Gaming Set" },
                    Editions: new[] { "Esports Pack", "Home Pack" }),

                new(
                    Key: "projector",
                    DisplayName: "May chieu & Man hinh",
                    MatchKeyword: "may chieu",
                    BasePrice: 200_000,
                    PriceStep: 30_000,
                    Locations: new[] { "Ha Noi - Hai Ba Trung", "TP. HCM - Quan 2", "Da Nang - Ngu Hanh Son", "Ha Noi - Tay Ho" },
                    Models: new[] { "BenQ TK850i", "Epson EF-21", "XGIMI Horizon", "Anker Nebula Cosmos", "ViewSonic PX748", "LG CineBeam Q", "Samsung Freestyle", "Hisense C1 Laser", "Optoma UHD38", "ASUS ZenBeam L2" },
                    Editions: new[] { "Cinema Pack", "Presentation Pack" }),

                new(
                    Key: "tablet",
                    DisplayName: "Tablet & iPad",
                    MatchKeyword: "tablet",
                    BasePrice: 150_000,
                    PriceStep: 22_000,
                    Locations: new[] { "Da Nang - Thanh Khe", "TP. HCM - Quan 7", "Ha Noi - Hoang Mai", "Ha Noi - Cau Giay" },
                    Models: new[] { "iPad Pro 13 M4", "iPad Air M2", "Galaxy Tab S9 Ultra", "Xiaomi Pad 6S Pro", "Lenovo Tab P12 Pro", "Huawei MatePad Pro", "Surface Pro 10", "iPad Mini 7", "Galaxy Tab S9 FE", "Honor Pad V9" },
                    Editions: new[] { "Creator Bundle", "Learning Bundle" }),

                new(
                    Key: "phone",
                    DisplayName: "Dien thoai",
                    MatchKeyword: "dien thoai",
                    BasePrice: 110_000,
                    PriceStep: 18_000,
                    Locations: new[] { "Ha Noi - Nam Tu Liem", "TP. HCM - Go Vap", "Da Nang - Hai Chau", "Ha Noi - Dong Da" },
                    Models: new[] { "iPhone 16 Pro Max", "Galaxy S24 Ultra", "Pixel 9 Pro", "Xiaomi 15 Ultra", "OnePlus 13", "Vivo X200 Pro", "OPPO Find X8", "Honor Magic 7", "ASUS ROG Phone 9", "Sony Xperia 1 VI" },
                    Editions: new[] { "256GB", "512GB" }),

                new(
                    Key: "network",
                    DisplayName: "Thiet bi mang",
                    MatchKeyword: "thiet bi mang",
                    BasePrice: 90_000,
                    PriceStep: 16_000,
                    Locations: new[] { "Ha Noi - Cau Giay", "TP. HCM - Quan 7", "Da Nang - Ngu Hanh Son", "Thu Duc" },
                    Models: new[] { "TP-Link Deco XE75", "ASUS ZenWiFi XT9", "Netgear Orbi RBK763", "UniFi Dream Router", "MikroTik hAP ax3", "Huawei 5G CPE Pro", "Starlink Standard Kit", "TP-Link Archer AXE75", "Linksys Atlas Pro", "Ubiquiti U7 Pro" },
                    Editions: new[] { "Office Kit", "Home Kit" }),
            };
        }

        private sealed record CategorySeedDefinition(
            string Key,
            string DisplayName,
            string MatchKeyword,
            decimal BasePrice,
            decimal PriceStep,
            string[] Locations,
            string[] Models,
            string[] Editions);
    }
}
