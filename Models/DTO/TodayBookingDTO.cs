namespace reservationSystem.Models.DTO
{
    public class TodayBookingDTO
    {
        public Guid Id { get; set; }
        public string GuestName { get; set; }
        public string Rooms { get; set; }
        public decimal Balance { get; set; }
        public string? CheckedInSince { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
    }
}
