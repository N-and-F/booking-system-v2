namespace reservationSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Role { get; set; } = 3;
        public int HotelId { get; set; }
        public DateTime? LastLoggedIn { get; set; }
        public Hotel Hotel { get; set; } = null!;
    }
}
