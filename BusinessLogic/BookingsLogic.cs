using reservationSystem.Models;
using reservationSystem.Data;
using reservationSystem.Enums;
using Microsoft.EntityFrameworkCore;
using reservationSystem.Models.DTO;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;


namespace reservationSystem.BusinessLogic
{
    public class BookingsLogic
    {
        private readonly DataSet _context;
        [Inject] public ILocalStorageService _localstorage { get; set; }
        [Inject] public AccountsLogic _accountsLogic { get; set; }

        public List<string>CountryList = new List<string>
        {
            "United States", "China", "India", "Brazil", "Russia", "Mexico", "Germany", "United Kingdom", "France", "Italy",
            "Canada", "Australia", "Spain", "Japan", "South Africa", "Argentina", "South Korea", "Netherlands", "Turkey", "Iran",
            "Thailand", "Nigeria", "Sweden", "Saudi Arabia", "Poland", "Belgium", "Switzerland", "Indonesia", "Austria", "Malaysia",
            "Colombia", "Philippines", "Ukraine", "Greece", "Norway", "Israel", "Denmark", "Ireland", "Romania", "Algeria",
            "Morocco", "Portugal", "Egypt", "Tunisia", "Sudan", "Libya", "Syria", "Jordan", "Iraq", "Kuwait", "Oman",
            "Qatar", "Bahrain", "United Arab Emirates", "Yemen", "Afghanistan", "Sri Lanka", "Bangladesh", "Nepal", "Pakistan", "Bhutan",
            "Maldives", "Cambodia", "Laos", "Vietnam", "Myanmar", "Thailand", "Malaysia", "Singapore", "Brunei", "Indonesia", "Philippines",
            "East Timor", "Papua New Guinea", "Solomon Islands", "Vanuatu", "Fiji", "New Zealand", "Australia", "Micronesia", "Kiribati", "Tonga",
            "Samoa", "Nauru", "Tuvalu", "Marshall Islands", "Palau", "Micronesia", "Guam", "Northern Mariana Islands", "American Samoa", "Cook Islands",
            "Niue", "Tokelau", "French Polynesia", "New Caledonia", "French Guiana", "Guadeloupe", "Martinique", "Guyana", "Suriname", "Grenada",
            "Barbados", "Antigua and Barbuda", "Dominica", "Saint Lucia", "Saint Vincent and the Grenadines", "Trinidad and Tobago", "Grenada", "Barbados", "Antigua and Barbuda", "Dominica",
            "Saint Lucia", "Saint Vincent and the Grenadines", "Trinidad and Tobago", "Grenada", "Barbados", "Antigua and Barbuda", "Dominica", "Saint Lucia", "Saint Vincent and the Grenadines", "Trinidad and Tobago"
        };

        public BookingsLogic(DataSet context, ILocalStorageService LocalStorage, AccountsLogic AccountsLogic)
        {
            _context = context;
            _localstorage = LocalStorage;
            _accountsLogic = AccountsLogic;
            
        }

        public async Task<List<Booking>> GetAllBookings()
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var bookings = await _context.Bookings
                .Where(x => x.HotelId == hotelId)
                .Include(x => x.BookingRooms)
                .ThenInclude(x => x.Room)
                .ToListAsync();

            return bookings;
		}

        public List<Booking> GetOverlappingBookings(DateTime startDate, DateTime endDate, List<Booking> allBookings)
        {
            var bookings = allBookings
                    .Where(x => x.StartDate.Date <= endDate && startDate <= x.EndDate.Date)
                    .ToList();

            return bookings;
        }

        public async Task<List<Booking>> GetTableItems(bool isActiveBookings = true)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var dateNow = DateTime.Now.Date;
            var bookings = await _context.Bookings.Where(x => x.HotelId == hotelId)
                .Include(x => x.BookingRooms)
                .ThenInclude(x => x.Room)
                .ToListAsync();
            if (isActiveBookings)
            {
                bookings = bookings
                    .Where(b => b.StartDate.Date > dateNow || b.EndDate.Date >= dateNow)
                    .OrderBy(b => b.StartDate)
                    .ToList();
            }
            else
            {
                bookings = bookings
                    .Where(b => b.EndDate.Date < dateNow)
                    .OrderByDescending(b => b.StartDate)
                    .ToList();
            }

            return bookings;
                
        }

        public async Task<List<BookingRoom>> GetBookingRooms()
        {
			var bookingRooms = await _context.BookingRooms
				.ToListAsync();

			return bookingRooms;
		}

		public async Task<List<TodayBookingDTO>> GetTodayBookings(bool isCheckIn)
		{
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var today = DateTime.Now.Date;
			var query = _context.Bookings
				.Where(b => ((isCheckIn && today >= b.StartDate.Date && today < b.EndDate.Date) || (!isCheckIn && b.EndDate.Date == today)) &&
                                b.HotelId == hotelId)
                .OrderByDescending(b => b.StartDate)
				.Include(b => b.Guest)
				.Include(b => b.BookingRooms)
					.ThenInclude(br => br.Room)
				.Select(b => new TodayBookingDTO
				{
					Id = b.Id,
					GuestName = b.Guest.Name ?? "N/A",
					Rooms = string.Join(", ", b.BookingRooms.Select(br => br.Room.Name)),
					Balance = b.Total - b.Paid,
                    CheckedInSince = FormatCheckedInSince(b.StartDate),
                    Notes = b.Notes
				});

			return await query.ToListAsync();
		}


		public async Task<BookingDTO> GetBookingDetails(string bID)
        {
            var bookingId = Guid.Parse(bID);
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
            if (booking == null) { return new BookingDTO(bookingId); } // return an empty BookingDTO object

            BookingDTO dto = new(booking);
            dto.RoomIds = await _context.BookingRooms
                .Where(x => x.BookingId == bookingId)
                .Select(x => x.RoomId)
                .ToListAsync();
            dto.BookingPayments = await _context.BookingPayments
                .Where(x => x.BookingId == bookingId)
                .ToListAsync();

            return dto;
        }

        public async Task<List<BookingPayment>> GetBookingPayments(Guid bookingId)
        {
           var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
            if (booking == null) { return new List<BookingPayment>(); }

            var bookingPayments = await _context.BookingPayments
                .Where(x => x.BookingId == bookingId)
                .ToListAsync();
            return bookingPayments;
        }

        public List<Booking> GetTableItemsFiltered(List<Booking> bookings, List<Guest> _guests, string filter)
        {
            var filteredRooms = new List<Booking>(bookings);

            if (!string.IsNullOrEmpty(filter))
            {
                var lowerFilter = filter.ToLower();
                var guests = _guests.Where(x => x.Name.ToLower().Contains(lowerFilter)).Select(x => x.Id).ToList();
                filteredRooms = filteredRooms.Where(u => guests.Contains(u.GuestId))
                                             .ToList();
            }

            return filteredRooms;
        }

        public async Task<IResult> HandleSaveBooking(BookingDTO dto, BookingDTO oldData)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (booking == null)
            {
                await CreateNewBooking(dto);
            }
            else
            {
                await EditBooking(dto, oldData);
            }
            return Results.Ok(dto);
        }

        public async Task<IResult> CreateNewBooking(BookingDTO newBooking)
        {
            try
            {
                // booking
                var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
                var id = int.Parse(await _localstorage.GetItemAsStringAsync("Id"));
                var booking = newBooking.MapBooking();
                booking.HotelId = hotelId;
                booking.CreatedBy = id;
                booking.CreatedOn = DateTime.Now;
                await _context.Bookings.AddAsync(booking);

                // booking rooms
                List<BookingRoom> bookingRooms = new();
                foreach(var roomId in newBooking.RoomIds)
                {
                    BookingRoom newBookingRoom = new();
                    newBookingRoom.RoomId = roomId;
                    newBookingRoom.BookingId = booking.Id;
                    bookingRooms.Add(newBookingRoom);
                    
                }
                await _context.BookingRooms.AddRangeAsync(bookingRooms);

                // booking payment history
                BookingPayment newBookingPayment = new()
                {
                    BookingId = booking.Id,
                    Amount = booking.Paid,
                    Date = DateTime.Now,
                    Notes = "Initial payment",
                };
                await _context.BookingPayments.AddAsync(newBookingPayment);

                // transaction
                var description = $"Created {booking.Guest.Name}'s new booking with initial payment of ₱{FormatDecimal(booking.Paid)}.";
                await _accountsLogic.MakeTransaction(booking.AccountId, booking.Paid, description, (int)TransactionTypes.HotelIncome, null);

                await _context.SaveChangesAsync();
                return Results.Ok(newBooking);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> EditBooking(BookingDTO dto, BookingDTO oldData)
        {
            try
            {
                var id = int.Parse(await _localstorage.GetItemAsStringAsync("Id"));
                var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == dto.Id);
                booking.Id = dto.Id;
                booking.EndDate = dto.EndDate;
                booking.StartDate = dto.StartDate;
                booking.Notes = dto.Notes;
                booking.Paid = dto.Paid;
                booking.Total = dto.Total;
                booking.GuestId = dto.GuestId;
                booking.AddOns = dto.AddOns;
                booking.Deductions = dto.Deductions;
                booking.UpdatedBy = id;
                booking.UpdatedOn = DateTime.Now;
                booking.AccountId = dto.AccountId;
                booking.Account = dto.Account;

                // booking rooms
                var existingBookingRooms = await _context.BookingRooms.Where(x => x.BookingId == booking.Id).ToListAsync();
                _context.BookingRooms.RemoveRange(existingBookingRooms);

                List<BookingRoom> bookingRooms = new();
                foreach (var roomId in dto.RoomIds)
                {
                    BookingRoom newBookingRoom = new();
                    newBookingRoom.RoomId = roomId;
                    newBookingRoom.BookingId = booking.Id;
                    bookingRooms.Add(newBookingRoom);

                }
                await _context.BookingRooms.AddRangeAsync(bookingRooms);

                // booking payment history
                BookingPayment newBookingPayment = new()
                {
                    BookingId = booking.Id,
                    Amount = booking.Paid,
                    Date = DateTime.Now,
                    Notes = "Initial payment edited",
                };
                await _context.BookingPayments.AddAsync(newBookingPayment);

                // transaction
                var description = "";
                if(oldData.AccountId != booking.AccountId && oldData.Paid != booking.Paid)
                {
                    description = $"{booking.Guest.Name}'s booking payment account was changed to {booking.Account.Name}. Payment was changed to ₱{FormatDecimal(booking.Paid)}.";
                    await _accountsLogic.MakeTransaction(oldData.AccountId, -oldData.Paid, description, (int)TransactionTypes.Transfer, null);
                    await _accountsLogic.MakeTransaction(booking.AccountId, booking.Paid, description, (int)TransactionTypes.Transfer, null);
                }
                if (oldData.AccountId != booking.AccountId && oldData.Paid == booking.Paid)
                {
                    description = $"{booking.Guest.Name}'s booking payment account was changed to {booking.Account.Name}.";
                    await _accountsLogic.MakeTransaction(booking.AccountId, booking.Paid, description, (int)TransactionTypes.Transfer, null);
                    await _accountsLogic.MakeTransaction(oldData.AccountId, -booking.Paid, description, (int)TransactionTypes.Transfer, null);

                }
                else if(oldData.Paid != booking.Paid)
                {
                    description = $"{booking.Guest.Name}'s booking payment was changed to ₱{FormatDecimal(booking.Paid)} from ₱{FormatDecimal(oldData.Paid)}.";
                    await _accountsLogic.MakeTransaction(oldData.AccountId, -oldData.Paid, description, (int)TransactionTypes.Transfer, null);
                    await _accountsLogic.MakeTransaction(booking.AccountId, booking.Paid, description, (int)TransactionTypes.Transfer, null);
                }
                

                await _context.SaveChangesAsync();
                return Results.Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> UpdatePayment(BookingPayment payment, int selectedAccountId)
        {
            var id = int.Parse(await _localstorage.GetItemAsStringAsync("Id"));
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == payment.BookingId);
            if (booking == null) { return Results.NotFound(); }
            await _context.BookingPayments.AddAsync(payment);
            booking.Paid += payment.Amount;
            booking.UpdatedBy = id;
            booking.UpdatedOn = DateTime.Now;

            var description = $"{booking.Guest.Name}'s booking payment was updated. ₱{FormatDecimal(payment.Amount)} has been added.";
            await _accountsLogic.MakeTransaction(selectedAccountId, payment.Amount, description, (int)TransactionTypes.HotelIncome, null);


            await _context.SaveChangesAsync();
            return Results.Ok();
            

        }

        public async Task<IResult> DeleteBooking(Guid id)
        {
            try
            {
                var booking = await _context.Bookings.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (booking == null) return Results.NotFound(id);

                var existingBookingRooms = await _context.BookingRooms.Where(x => x.BookingId == booking.Id).ToListAsync();
                _context.BookingRooms.RemoveRange(existingBookingRooms);

                var existingBookingPayment = await _context.BookingPayments.Where(x => x.BookingId == booking.Id).ToListAsync();
                _context.BookingPayments.RemoveRange(existingBookingPayment);

                var description = $"{booking.Guest.Name}'s booking has been deleted. Total amount paid is removed from {booking.Account.Name}.";
                await _accountsLogic.MakeTransaction(booking.AccountId, -booking.Paid, description, (int)TransactionTypes.Transfer, null);

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                return Results.Ok(booking);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<int> IsDatesAvailable(string Id, DateTime StartDate, DateTime EndDate, List<int> RoomIds)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var bookingRooms = await _context.BookingRooms.ToListAsync();
            var bookings = await _context.Bookings.Where(b => b.Id != Guid.Parse(Id) && b.HotelId == hotelId).ToListAsync();
            var rooms = await _context.Rooms.ToListAsync();

            foreach (var roomId in RoomIds)
            {
                // Get the room entity to check if it's a double room
                var room = rooms.FirstOrDefault(x=>x.Id == roomId);
                if (room == null) continue; // Skip if room not found

                // Determine the rooms to check for availability
                var roomsToCheck = new List<int> { roomId };
                if (room.OriginalId.HasValue)
                {
                    roomsToCheck.Add(room.OriginalId.Value);
                }
                var duplicateRoomId = rooms.Where(x => x.OriginalId == roomId).Select(x => x.Id).FirstOrDefault();
                if (duplicateRoomId != 0)
                {
                    roomsToCheck.Add(duplicateRoomId);
                }


                // Check if there are any bookings for the rooms that overlap with the requested dates
                var isRoomUnavailable = bookingRooms
                    .Where(br => roomsToCheck.Contains(br.RoomId))
                    .Join(bookings, br => br.BookingId, b => b.Id, (br, b) => b)
                    .Any(b => b.StartDate <= EndDate.AddDays(-1) && b.EndDate > StartDate);

                if (isRoomUnavailable)
                {
                    return roomId; // Return the ID of the first room in the list as an example of an unavailable room
                }

            }
            return 0;
        }

        public MostBookedRoomDTO GetMostBookedRoom(DateTime startDate, DateTime endDate, List<Booking> bookings)
        {
            try
            {
                Dictionary<string, int> roomsCount = new Dictionary<string, int>();
                foreach (var booking in bookings)
                {
                    int surplusDays = 0;
                    if (booking.StartDate < startDate)
                    {
                        surplusDays = (startDate - booking.StartDate.Date).Days;
                    }
                    if (booking.EndDate > endDate)
                    {
                        surplusDays += (booking.EndDate.Date - endDate).Days;
                    }
                    var daysDiff = (booking.EndDate - booking.StartDate).Days - surplusDays;
                    foreach (var bookingRoom in booking.BookingRooms)
                    {
                        if (roomsCount.ContainsKey(bookingRoom.Room.Name))
                        {
                            roomsCount[bookingRoom.Room.Name] += daysDiff;
                        }
                        else
                        {
                            roomsCount.Add(bookingRoom.Room.Name, daysDiff);
                        }
                    }
                        
                }

                if (roomsCount.Count > 0)
                {
                    KeyValuePair<string, int> kvpWithMaxValue = roomsCount.Aggregate((a, b) => a.Value > b.Value ? a : b);
                    return new MostBookedRoomDTO
                    {
                        RoomName = kvpWithMaxValue.Key,
                        TotalBookings = kvpWithMaxValue.Value
                    };
                }
           
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return new MostBookedRoomDTO
            {
                RoomName = "N/A",
                TotalBookings = 0
            };
        }
        public (int, int) GetTotalBookings(DateTime startDate, DateTime endDate, List<Booking> allBookings)
        {
            try
            {
                // Get all bookings from the database that has start dates in between the two parameters
                var bookings = allBookings.Where(x => x.StartDate >= startDate && 
                                                                  x.StartDate <= endDate.AddDays(-1)).ToList(); 

                // Calculate the total number of bookings
                int totalBookings = bookings.Count;

                int totalDaysBooked = GetNumberOfRoomBookings(startDate, endDate, bookings);

                return (totalBookings, totalDaysBooked);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // Handle exception appropriately
                return (0, 0);
            }
        }
        public int GetNumberOfRoomBookings(DateTime startDate, DateTime endDate, List<Booking> bookings)
        {
            try
            {

                Dictionary<string, int> roomsCount = new Dictionary<string, int>();
                foreach (var booking in bookings)
                {
                    int surplusDays = 0;
                    if(booking.StartDate < startDate)
                    {
                        surplusDays = (startDate - booking.StartDate.Date).Days;
                    }
                    if (booking.EndDate > endDate)
                    {
                        surplusDays += (booking.EndDate.Date - endDate).Days;
                    }
                    var daysDiff = (booking.EndDate - booking.StartDate).Days - surplusDays;
                    foreach (var bookingRoom in booking.BookingRooms)
                    {
                        if (roomsCount.ContainsKey(bookingRoom.Room.Name))
                        {
                            roomsCount[bookingRoom.Room.Name] += daysDiff;
                        }
                        else
                        {
                            roomsCount.Add(bookingRoom.Room.Name, daysDiff);
                        }
                    }
                }

               int totalBookings = roomsCount.Values.Sum();

                return totalBookings;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // Handle exception appropriately
                return 0;
            }
        }


        public double CalculateBookingPercentage(DateTime startDate, DateTime endDate, int roomCount)
        {
            try
            {
                
                int totalDays = (endDate - startDate).Days;
                double totalRooms = 11 * totalDays;

                var roomPercentage = (roomCount / totalRooms) * 100;
                double roundedPercentage = Math.Round(roomPercentage, 2);

                return roundedPercentage;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        public decimal CalculateTotalRevenueInPeso(DateTime startDate, DateTime endDate, List<Booking> allBookings)
        {
            try
            {
                var bookings = allBookings
                    .Where(x => x.StartDate >= startDate && 
                                x.EndDate <= endDate)
                    .ToList();

                decimal totalRevenue = bookings.Sum(b => b.Paid);
                decimal roundedRevenue = Math.Round(totalRevenue, 2);

                return roundedRevenue;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        public async Task<decimal> ConvertRevenueToUSD(decimal totalRevenue)
        {
            try
            {
                string apiUrl = "https://api.exchangerate-api.com/v4/latest/USD";
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var exchangeRates = System.Text.Json.JsonDocument.Parse(jsonString).RootElement.GetProperty("rates");

                        decimal exchangeRatePHPtoUSD = exchangeRates.GetProperty("PHP").GetDecimal();

                        decimal totalRevenueInUSD = totalRevenue / exchangeRatePHPtoUSD;
                        decimal roundedRevenueInUSD = Math.Round(totalRevenueInUSD, 2);

                        return roundedRevenueInUSD;
                    }
                    else
                    {
                        throw new Exception("Failed to fetch exchange rates from API.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        public static string FormatDecimal(decimal value)
        {
            return value.ToString("N2", new System.Globalization.CultureInfo("en-US"));
        }

        public static string FormatCheckedInSince(DateTime startDate)
        {
            if (startDate.Date == DateTime.Today) return "Today";
            if (startDate.Date == DateTime.Today.AddDays(-1)) return $"Yesterday, {startDate.ToString("MM/dd/yyyy")}";
            var diff = DateTime.Today - startDate.Date;
            if (diff.Days < 7) return startDate.ToString("dddd, MM/dd/yyyy");
            return startDate.ToString("MM/dd/yyyy");
        }

    }
}
