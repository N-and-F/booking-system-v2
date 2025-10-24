using System.ComponentModel.DataAnnotations.Schema;

namespace reservationSystem.Models
{
    public class BookingRoom
    {
		[ForeignKey("Room")]
		public int RoomId { get; set; }

		[ForeignKey("Booking")]
		public Guid BookingId { get; set; }

		// Navigation properties
		public Room Room { get; set; }
		public Booking Booking { get; set; }
	}
}
