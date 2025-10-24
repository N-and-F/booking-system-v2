namespace reservationSystem.Models
{
	public class CalendarItem
	{
		public DateTime Date { get; set; }
		public string RoomNames { get; set; }

		public CalendarItem(DateTime date, string rooms) 
		{
			Date = date;
			RoomNames = rooms;
		}

	}
}
