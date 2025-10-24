namespace reservationSystem.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Price { get; set; } = 0;
        public int NumGuests { get; set; }
        public int? OriginalId { get; set; }
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = null!;
		public ICollection<BookingRoom> BookingRooms { get; set; }
        public RoomType RoomType { get; set; } = null!;

	}
}
