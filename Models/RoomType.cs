namespace reservationSystem.Models
{
    public class RoomType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; }
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
