using reservationSystem.Models;
using reservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using reservationSystem.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace reservationSystem.BusinessLogic
{
    public class HotelsLogic
    {
        private readonly DataSet _context;

        public HotelsLogic(DataSet context)
        {
            _context = context;
        }
        public async Task<List<Hotel>> GetHotels()
        {
            return await _context.Hotels.OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<Hotel?> GetHotel(int id)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(x => x.Id == id);
            if (hotel == null) { return null; }
            return hotel;
        }

        public async Task<IResult> CreateNewUser(User newUser)
        {
            try
            {
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                return Results.Ok(newUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> EditUser(User u)
        {
            try
            {
                var user = await _context.Users.Where(x => x.Id == u.Id).FirstOrDefaultAsync();
                if (user == null) return Results.NotFound(u.Id);

                user.Username = u.Username;
                user.Role = u.Role;
                user.Password = u.Password;

                await _context.SaveChangesAsync();
                return Results.Ok(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }
    }
}
