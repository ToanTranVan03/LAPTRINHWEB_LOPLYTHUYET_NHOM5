using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TechShare.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty; // Gán mặc định để tránh lỗi null
        public string? Address { get; set; }
        
        // Thông tin xác thực (KYC)
        public string? CCCD_Image_Front { get; set; }
        public string? CCCD_Image_Back { get; set; }
        public bool IsVerified { get; set; } = false;
    }
}