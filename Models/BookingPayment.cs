namespace reservationSystem.Models
{
    public class BookingPayment
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
    }
}
