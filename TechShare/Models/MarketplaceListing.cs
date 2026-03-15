using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShare.Models
{
    public enum ListingStatus { Active, Sold, Cancelled }
    public enum ProductCondition { LikeNew, Good, Fair, Poor }

    public class MarketplaceListing
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng mô tả sản phẩm")]
        [StringLength(3000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(10000, 100000000, ErrorMessage = "Giá từ 10.000đ đến 100.000.000đ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tình trạng")]
        public ProductCondition Condition { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [NotMapped]
        public double? DistanceKm { get; set; }

        public ListingStatus Status { get; set; } = ListingStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Người bán
        [Required]
        public string SellerId { get; set; } = string.Empty;
        [ForeignKey("SellerId")]
        public virtual ApplicationUser? Seller { get; set; }

        // Danh mục
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        // Người mua (nếu đã bán)
        public string? BuyerId { get; set; }
        [ForeignKey("BuyerId")]
        public virtual ApplicationUser? Buyer { get; set; }
    }
}
