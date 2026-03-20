using System.ComponentModel.DataAnnotations;

namespace TechShare.ViewModels
{
    public class UserProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        public string UserNameOrEmail { get; set; } = string.Empty;
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string? PhoneNumber { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        public string? Address { get; set; }

        public bool IsVerified { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
    }
}
