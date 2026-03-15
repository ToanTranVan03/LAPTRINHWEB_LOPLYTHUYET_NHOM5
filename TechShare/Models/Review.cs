using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShare.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Khóa ngoại - Sản phẩm
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        // Khóa ngoại - Người đánh giá
        public string? ReviewerId { get; set; }
        [ForeignKey("ReviewerId")]
        public virtual ApplicationUser? Reviewer { get; set; }
    }
}
