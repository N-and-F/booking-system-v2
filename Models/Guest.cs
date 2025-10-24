using System.ComponentModel.DataAnnotations;

namespace reservationSystem.Models
{

    public class Guest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int? NumBookings { get; set; } = 0;
        public int HotelId { get; set; }

        public Hotel Hotel { get; set; } = null!;
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
