using TechShare.Models;

namespace TechShare.ViewModels
{
    public class HomeLandingViewModel
    {
        public IReadOnlyList<CategoryHighlightViewModel> HighlightCategories { get; set; } = [];
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalReviews { get; set; }
        public int TotalUsers { get; set; }
        public int TotalBookings { get; set; }
        public IReadOnlyList<Product> LatestProducts { get; set; } = [];
    }
}
