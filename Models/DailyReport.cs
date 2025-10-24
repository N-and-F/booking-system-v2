namespace reservationSystem.Models
{
    public class DailyReport
    {
        public DateTime Date { get; set; }
        public Dictionary<string, int> RoomGuestCount { get; set; } = [];
        public int NumGuestCheckIn { get; set; }
        public int NumGuestOvernight { get; set; }
        public int NumRoomsOccupied { get; set; }
    }
}
