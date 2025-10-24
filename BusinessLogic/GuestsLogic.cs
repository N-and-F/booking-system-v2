using reservationSystem.Models;
using reservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;

namespace reservationSystem.BusinessLogic
{
    public class GuestsLogic
    {
        private readonly DataSet _context;
        [Inject] public ILocalStorageService _localstorage { get; set; }
        public GuestsLogic(DataSet context, ILocalStorageService LocalStorage)
        {
            _context = context;
            _localstorage = LocalStorage;
        }

        public async Task<List<Guest>> GetTableItems(string? name = null)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var guests =  await _context.Guests
                .Where(g => g.HotelId == hotelId)
                .OrderBy(b => b.Name)
                .ToListAsync();
            if (name != null) 
            {
                var guestsList =  guests.Where(guest => guest.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)).OrderBy(x => x.Name).ToList();
                return guestsList;
            }
            return guests; 
        }

        public async Task<Guest> GetGuestDetails(int guestId)
        {
            var guest = await _context.Guests.FirstOrDefaultAsync(x => x.Id == guestId);

            if (guest == null)
            {
                return new Guest();
            }

            return guest;
        }

        public async Task<Guest> FindGuest(string name)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var guest = await _context.Guests
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower() &&
                                          x.HotelId == hotelId);
            if (guest == null)
            {
                return new Guest()
                {
                    Email = "",
                    Name = name,
                    Country = "Philippines",
                    HotelId = hotelId
                };
            }
            return guest;
        }


        public async Task<Guest> FindGuest(string name, string email, string country)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var guest = await _context.Guests
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower() &&
                                          x.HotelId == hotelId);
            
            if (guest == null)
            {
                TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
                return new Guest()
                {
                    Email = email,
                    Name = ti.ToTitleCase(name.ToLower()),
                    Country = country,
                    HotelId = hotelId
                };
            }
            return guest;
        }


        public async Task<int> SaveGuest(string name, string email, string country)
        {
            try
            {
                
                var guest = await FindGuest(name, email, country);
                guest.NumBookings += 1;
                if (guest.Id == 0)
                {
                    await CreateGuest(guest);
                }
                else
                {
                    await EditGuest(guest);
                }
                return guest.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
            
        }


        public async Task<IResult> CreateGuest(Guest guest)
        {
            try
            {
                await _context.Guests.AddAsync(guest);

                await _context.SaveChangesAsync();
                return Results.Ok(guest);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> EditGuest(Guest guest)
        {
            try
            {
                var guestDB = await _context.Guests.Where(x => x.Id == guest.Id).FirstOrDefaultAsync();
                if (guestDB == null) return Results.NotFound(guest.Id);

                guestDB.Name = guest.Name;
                guestDB.Email = guest.Email;
                guestDB.Country = guest.Country;
                guestDB.NumBookings = guest.NumBookings;

                await _context.SaveChangesAsync();
                return Results.Ok(guestDB);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> DeleteGuest(int id)
        {
            try
            {
                var guest = await _context.Guests.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (guest == null) return Results.NotFound(id);

                _context.Remove(guest);
                await _context.SaveChangesAsync();
                return Results.Ok(guest);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }
    }
}
