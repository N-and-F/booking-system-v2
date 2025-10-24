namespace reservationSystem.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; } = 0;
        public bool IsPrimary { get; set; }
        public ICollection<AccountTransaction> AccountTransactions { get; set; } = new List<AccountTransaction>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
