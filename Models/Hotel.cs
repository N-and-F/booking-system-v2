namespace reservationSystem.Models
{
    public class Hotel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PrimaryColor { get; set; }
        public string LogoUrl { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Guest> Guests { get; set; } = new List<Guest>();
        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    }
}
