using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShare.Models
{
    public enum BookingStatus { Pending, Approved, Rented, Completed, Cancelled }

    public class Booking
    {
        public int Id { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.Now;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public string? RenterId { get; set; }
        [ForeignKey("RenterId")]
        public virtual ApplicationUser? Renter { get; set; }
    }
}