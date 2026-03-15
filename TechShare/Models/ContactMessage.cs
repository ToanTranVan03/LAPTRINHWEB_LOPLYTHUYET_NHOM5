using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShare.Models
{
    public enum ContactStatus { New, Read, Replied, Closed }

    public class ContactMessage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn chủ đề")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ContactStatus Status { get; set; } = ContactStatus.New;

        // Người gửi (nếu đã đăng nhập)
        public string? SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }

        // Phản hồi từ admin
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
    }
}
