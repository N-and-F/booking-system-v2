using reservationSystem.Models;
using reservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using reservationSystem.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Blazored.LocalStorage;

namespace reservationSystem.BusinessLogic
{
    public class UsersLogic
    {
        private readonly DataSet _context;
        [Inject] public ILocalStorageService _localstorage { get; set; }
        public UsersLogic(DataSet context, ILocalStorageService LocalStorage)
        {
            _context = context;
            _localstorage = LocalStorage;
        }

        public string MapRole(int role)
        {
            if (Enum.IsDefined(typeof(RoleTypes), role))
            {
                return Enum.GetName(typeof(RoleTypes), role);
            }
            else
            {
                // Handle the case when the integer does not correspond to any enum value
                return "";
            }
        }

        public async Task<bool> IsUnique (string username, int id)
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var user = await _context.Users.FirstOrDefaultAsync (x => x.Username.ToLower() == username.ToLower() && x.HotelId == hotelId);
            if (user == null) return true;
            if (user.Id == id) return true;
            return false;
        }
        public async Task<List<User>> GetTableItems()
        {
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            var roleId = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
            var users = await _context.Users.Where(x => x.HotelId == hotelId).OrderBy(x => x.Username).ToListAsync();
            if (roleId != (int)RoleTypes.SuperAdmin)
                users = users.Where(x=>x.Role != (int)RoleTypes.SuperAdmin).ToList();
            return users;
        }

        public List<User> GetTableItemsFiltered(List<User> users, string filter)
        {
            var filteredUsers = new List<User>(users);

            if (!string.IsNullOrEmpty(filter))
            {
                var lowerFilter = filter.ToLower();
                filteredUsers = filteredUsers.Where(u => u.Username.ToLower().Contains(lowerFilter) ||
                                                          ("admin".Contains(lowerFilter) && u.Role == (int)RoleTypes.Admin) ||
                                                          ("manager".Contains(lowerFilter) && u.Role == (int)RoleTypes.Manager) ||
                                                          ("staff".Contains(lowerFilter) && u.Role == (int)RoleTypes.Staff))
                                             .ToList();
            }

            return filteredUsers;
        }

        public async Task<IResult> CreateNewUser(User newUser)
        {
            try
            {
                var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
                newUser.HotelId = hotelId;
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

        public async Task<IResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.Where(x => x.Id == id).FirstOrDefaultAsync();
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
    }
}
