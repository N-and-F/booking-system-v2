namespace reservationSystem.Models
{
    public class AccountTransaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public int Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Account Account { get; set; }
	}
}
