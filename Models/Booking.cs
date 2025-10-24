namespace reservationSystem.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public int GuestId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Notes { get; set; }
        public decimal Paid { get; set; }
        public decimal Total { get; set; }
        public decimal AddOns { get; set; }
        public decimal Deductions { get; set; }
        public int HotelId { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int AccountId { get; set; }

        public Guest Guest { get; set; } = null!;
        public Hotel Hotel { get; set; } = null!;
        public Account Account { get; set; } = null!;
		public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
		public ICollection<BookingPayment> BookingPayments { get; set; } = new List<BookingPayment>();
	}
}
