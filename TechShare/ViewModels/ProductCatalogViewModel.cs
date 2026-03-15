using Microsoft.AspNetCore.Mvc.Rendering;
using TechShare.Models;

namespace TechShare.ViewModels
{
    public class ProductCatalogViewModel
    {
        public IReadOnlyList<Product> Products { get; set; } = [];
        public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public string? SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }
}
