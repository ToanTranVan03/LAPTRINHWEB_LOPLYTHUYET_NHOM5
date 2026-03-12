using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShare.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PricePerDay { get; set; }
        public string? ImageUrl { get; set; }
        
        [Required]
        public string Location { get; set; } = string.Empty; // Vị trí để tìm kiếm gần đây
        
        public bool IsAvailable { get; set; } = true;

        // Khóa ngoại
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public string? OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }
    }
}