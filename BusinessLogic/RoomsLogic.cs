using reservationSystem.Models;
using reservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using reservationSystem.Enums;
using reservationSystem.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Blazored.LocalStorage;

namespace reservationSystem.BusinessLogic
{
    public class RoomsLogic
    {
        private readonly DataSet _context;
        [Inject] public ILocalStorageService _localstorage { get; set; }
        public RoomsLogic(DataSet context, ILocalStorageService LocalStorage)
        {
            _context = context;
            _localstorage = LocalStorage;
        }

        public string MapRoom(int role, int hotelId, List<RoomType> roomTypes)
        {
            var roomType = roomTypes.Where(x => x.HotelId == hotelId && x.Id == role).FirstOrDefault();
            if (roomType == null) return "";
            return roomType.Name;
        }

        public async Task<bool> IsUnique (string name, int id)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var room = await _context.Rooms.FirstOrDefaultAsync (x => x.Name.ToLower() == name.ToLower() && x.HotelId == hotelId);
            if (room == null) return true;
            if (room.Id == id) return true;
            return false;
        }
        public async Task<List<Room>> GetTableItems()
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            return await _context.Rooms.Where(x => x.HotelId == hotelId).OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<List<RoomType>> GetRoomTypes()
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            return await _context.RoomTypes.Where(x => x.HotelId == hotelId).ToListAsync();
        }

        public async Task<List<Room>> GetSingleRooms()
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            return await _context.Rooms.Where(x => x.Type == 0 &&
                                                   x.HotelId == hotelId)
                                       .OrderBy(x => x.Name)
                                       .ToListAsync();
        }

        public async Task<string> GetBookingRoomNames(Guid id)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var bookings = await _context.Bookings.Where(x => x.Id == id && x.HotelId == hotelId)
                                                  .Include(x => x.BookingRooms)
                                                  .ThenInclude(x => x.Room)
                                                  .FirstOrDefaultAsync();

            List<string> roomNames = new();
            if (bookings == null) return "";
            foreach (var bookingRoom in bookings.BookingRooms)
            {
                roomNames.Add(bookingRoom.Room.Name);
            }

            return string.Join(", ", roomNames);
        }
        public List<Room> GetTableItemsFiltered(List<Room> rooms, string filter)
        {
            var filteredRooms = new List<Room>(rooms);

            if (!string.IsNullOrEmpty(filter))
            {
                var lowerFilter = filter.ToLower();
                filteredRooms = filteredRooms.Where(u => u.Name.ToLower().Contains(lowerFilter))
                                             .ToList();
            }

            return filteredRooms;
        }

        public async Task<List<Room>> GetAvailableRooms(DateTime? startDate, DateTime? endDate, int hotelId)
        {
            
            var rooms = await _context.Rooms.Where(x => x.HotelId == hotelId).ToListAsync();
            var bookingRooms = await _context.BookingRooms.ToListAsync();
            var bookings = await _context.Bookings.Where(x => x.HotelId == hotelId).ToListAsync();
            var availableRooms = new List<Room>();
            var roomsToCheck = new List<int>();

            foreach (var room in rooms)
            {
                var isRoomUnavailable = bookingRooms
                   .Where(br => br.RoomId == room.Id)
                   .Join(bookings, br => br.BookingId, b => b.Id, (br, b) => b)
                   .Any(b => b.StartDate <= endDate?.AddDays(-1) && b.EndDate > startDate);

                if (isRoomUnavailable)
                {
                    // check if double room
                    if(room.OriginalId.HasValue)
                    {
                        roomsToCheck.Add(room.OriginalId.Value);
                        continue;
                    }

                    // check if single room
                    if(room.Type == 0)
                    {
                        var duplicateRoomId = rooms.Where(x => x.OriginalId == room.Id).Select(x => x.Id).FirstOrDefault();
                        roomsToCheck.Add(duplicateRoomId);
                    }
                }
                else
                {
                    availableRooms.Add(room);
                }
            }
            availableRooms.RemoveAll(room => roomsToCheck.Contains(room.Id));
            return availableRooms.OrderBy(x => x.Name).ToList();
        }

        public async Task<IResult> CreateNewRoom(Room newRoom)
        {
            try
            {
                var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
                newRoom.HotelId = hotelId;
                await _context.Rooms.AddAsync(newRoom);
                await _context.SaveChangesAsync();
                return Results.Ok(newRoom);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> CreateNewRoomType(RoomType newType)
        {
            try
            {
                await _context.RoomTypes.AddAsync(newType);
                await _context.SaveChangesAsync();
                return Results.Ok(newType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> EditRoom(Room r)
        {
            try
            {
                var room = await _context.Rooms.Where(x => x.Id == r.Id).FirstOrDefaultAsync();
                if (room == null) return Results.NotFound(r.Id);

                room.Name = r.Name;
                room.Type = r.Type;
                room.Price = r.Price;
                room.OriginalId = r.OriginalId;
                room.NumGuests = r.NumGuests;

                await _context.SaveChangesAsync();
                return Results.Ok(room);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> DeleteRoom(int id)
        {
            try
            {
                var user = await _context.Rooms.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (user == null) return Results.NotFound(id);

                _context.Remove(user);
                await _context.SaveChangesAsync();
                return Results.Ok(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<bool> DeleteRoomType(int id)
        {
            try
            {
                var roomType = await _context.RoomTypes.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (roomType == null) return false;

                var rooms = await _context.Rooms.Where(x => x.Type == id).ToListAsync();
                if (rooms.Any()) return false;

                _context.Remove(roomType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
