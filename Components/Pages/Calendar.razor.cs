using reservationSystem.Models;
using reservationSystem.BusinessLogic;
using reservationSystem.Enums;
using Microsoft.AspNetCore.Components;

namespace reservationSystem.Components.Pages
{
	public partial class Calendar
	{
		[Inject] private BookingsLogic BookingsLogic { get; set; }
		[Inject] private RoomsLogic RoomsLogic { get; set; }
		public bool IsLoading { get; set; } = false;
		public string HotelColor { get; set; }
		string monthName = "";
		DateTime monthEnd;
		int monthsAway = 0;
		int numDummyColumn = 0;
		int year = DateTime.Now.Year;
		int month = 0;
		List<CalendarItem> items = new List<CalendarItem>();
		IEnumerable<CalendarItem> GroupedItems = new List<CalendarItem>();
		List<string> dayOfWeek =
		[
			"Sun",
			"Mon",
			"Tue",
			"Wed",
			"Thu",
			"Fri",
			"Sat",
		];
		protected override async Task OnInitializedAsync()
		{
			IsLoading = true;
			await PopulateBookedDates();
            HotelColor = await _localstorage.GetItemAsStringAsync("HotelColor");
            HotelColor = HotelColor.Replace('\"', ' ').Trim();
            CreateMonth();
			IsLoading = false;
		}

		void CreateMonth()
		{
			var tempDate = DateTime.Now.AddMonths(monthsAway);
			month = tempDate.Month;
			year = tempDate.Year;

			DateTime monthStart = new DateTime(year, month, 1);
			monthEnd = monthStart.AddMonths(1).AddDays(-1);
			monthName = monthStart.Month switch
			{
				1 => "January",
				2 => "February",
				3 => "March",
				4 => "April",
				5 => "May",
				6 => "June",
				7 => "July",
				8 => "August",
				9 => "September",
				10 => "October",
				11 => "November",
				12 => "December",
				_ => ""
			};

			numDummyColumn = (int)monthStart.DayOfWeek;
		}

		async Task PopulateBookedDates()
		{
			var bookings = await BookingsLogic.GetAllBookings();
			var bookingsRooms = await BookingsLogic.GetBookingRooms();
			var rooms = await RoomsLogic.GetTableItems();

			foreach (var booking in bookings)
			{
				var roomIds = bookingsRooms.Where(x => x.BookingId == booking.Id)
										   .Select(x => x.RoomId)	
										   .ToList();
				var roomNames = rooms.Where(x => roomIds.Contains(x.Id)).Select(x=>x.Name).ToList();
				var roomNamesJoined = string.Join(", ", roomNames);
				var diff = booking.EndDate - booking.StartDate;
				var days = diff.Days;

				for (var day = 0; day < days; day++)
				{
					var currDay = booking.StartDate.AddDays(day);
					items.Add(new CalendarItem(currDay, roomNamesJoined));
				}
			}

			GroupedItems = items.GroupBy(item => item.Date)
								.Select(group => new CalendarItem(group.Key, string.Join(", ", group.Select(item => item.RoomNames))));
		}
	}
}