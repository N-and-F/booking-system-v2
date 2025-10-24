using reservationSystem.Data;

namespace reservationSystem.Models.DTO
{
    public class BookingDTO
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
        public List<int> RoomIds { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }
        public List<BookingPayment> BookingPayments { get; set; }
        public Booking MapBooking()
        {

            Booking booking = new();
            booking.Id = this.Id;
            booking.EndDate = this.EndDate;
            booking.StartDate = this.StartDate;
            booking.Notes = this.Notes;
            booking.Paid = this.Paid;
            booking.Total = this.Total;
            booking.GuestId = this.GuestId;
            booking.AddOns = this.AddOns;
            booking.Deductions = this.Deductions;
            booking.AccountId = this.AccountId;
            booking.Account = this.Account;
            return booking;
        }
        public BookingDTO(Guid id)
        {
            this.Id = id;
            this.StartDate = DateTime.Now;
            this.EndDate = DateTime.Now.AddDays(1);
            this.RoomIds = [];
        }
        public BookingDTO(Booking booking)
        {
            Id = booking.Id;
            GuestId = booking.GuestId;
            StartDate = booking.StartDate;
            EndDate = booking.EndDate;
            Notes = booking.Notes;
            Paid = booking.Paid;
            Total = booking.Total;
            AddOns = booking.AddOns;
            Deductions = booking.Deductions;
            CreatedBy = booking.CreatedBy;
            CreatedOn = booking.CreatedOn;
            UpdatedBy = booking.UpdatedBy;
            UpdatedOn = booking.UpdatedOn;
            AccountId = booking.AccountId;
            Account = booking.Account;
        }
        public BookingDTO(BookingDTO booking)
        {
            Id = booking.Id;
            GuestId = booking.GuestId;
            StartDate = booking.StartDate;
            EndDate = booking.EndDate;
            Notes = booking.Notes;
            Paid = booking.Paid;
            Total = booking.Total;
            AddOns = booking.AddOns;
            Deductions = booking.Deductions;
            CreatedBy = booking.CreatedBy;
            CreatedOn = booking.CreatedOn;
            UpdatedBy = booking.UpdatedBy;
            UpdatedOn = booking.UpdatedOn;
            AccountId = booking.AccountId;
            Account = booking.Account;
        }
    }
}
