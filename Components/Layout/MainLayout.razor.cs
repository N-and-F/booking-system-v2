using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using reservationSystem.BusinessLogic;
using reservationSystem.Components.Pages;
using reservationSystem.Data;
using reservationSystem.Models;
using System.Text.Json;


namespace reservationSystem.Components.Layout
{
    public partial class MainLayout
    {
        public bool IsLoggedIn { get; set; } = false;
        public bool IsLoading { get; set; } = false;
        public string UserName { get; set; } = string.Empty;
        public int Role { get; set; } = 2;
        public int HotelId { get; set; }
        public List<Hotel> HotelList { get; set; }
        bool DrawerOpen { get; set; } = true;
        public string HotelColor { get; set; }
        public string HotelLogo { get; set; }
        [Inject] public HotelsLogic HotelsLogic { get; set; }

        [Inject] public DataSet _context { get; set; }
        private bool hasRendered = false;

        [Inject]
        private NavigationManager? NavigationManager { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            HotelList = await HotelsLogic.GetHotels();
            try
            {
                UserName = await _localstorage.GetItemAsStringAsync("Username");
                Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
                HotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
                HotelColor = await _localstorage.GetItemAsStringAsync("HotelColor");
                HotelColor = HotelColor.Replace('\"', ' ').Trim();
                HotelLogo = await _localstorage.GetItemAsStringAsync("HotelLogo");
                HotelLogo = HotelLogo.Replace('\"', ' ').Trim();
                IsLoggedIn = true;
                IsLoading = false;

                var id = int.Parse(await _localstorage.GetItemAsStringAsync("Id"));
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
                if (user != null)
                {
                    user.LastLoggedIn = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                StateHasChanged();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: ", ex.Message);
            }
            IsLoading = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // JavaScript interop calls should only be performed after the component has rendered
                await Initialize();
                hasRendered = true;
                StateHasChanged();
            }
        }

        private async Task HandleHotelChange(int hotelId)
        {
            IsLoading = true;
            HotelId = hotelId;
            await _localstorage.SetItemAsync("HotelId", HotelId);

            var hotel = HotelList.FirstOrDefault(x => x.Id == HotelId);
            HotelColor = hotel.PrimaryColor;
            HotelLogo = hotel.LogoUrl;
            await _localstorage.SetItemAsync("HotelColor", hotel.PrimaryColor);
            await _localstorage.SetItemAsync("HotelLogo", hotel.LogoUrl);
            StateHasChanged();
            IsLoading = false;
        }

        private async Task Initialize()
        {
            IsLoading = true;
            string id = await _localstorage.GetItemAsStringAsync("Id");
            

            if (string.IsNullOrEmpty(UserName))
            {
                IsLoggedIn = false;
                IsLoading = false;
                return;
            }
            var user = await _context.Users.Where(x => x.Id == int.Parse(id)).FirstOrDefaultAsync();
            if (user == null)
            {
                IsLoggedIn = false;
                IsLoading = false;
                return;
            }
            IsLoggedIn = true;
            IsLoading = false;
            return;
        }

        

        void ToggleDrawer()
        {
            DrawerOpen = !DrawerOpen;
        }

        public async Task HandleLoginSuccess(bool success)
        {
            IsLoading = true;
            
            if (success)
            {
                UserName = await _localstorage.GetItemAsStringAsync("Username");
                Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
                HotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));

                HotelColor = await _localstorage.GetItemAsStringAsync("HotelColor");
                HotelColor = HotelColor.Replace('\"', ' ').Trim();
                HotelLogo = await _localstorage.GetItemAsStringAsync("HotelLogo");
                HotelLogo = HotelLogo.Replace('\"', ' ').Trim();
                var id = int.Parse(await _localstorage.GetItemAsStringAsync("Id"));
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
                if (user != null)
                {
                    user.LastLoggedIn = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                StateHasChanged();
            }
            IsLoggedIn = success;
            IsLoading = false;

        }

        async void HandleLogoutSuccess()
        {
            if (IsLoggedIn)
            {
                await _localstorage.RemoveItemAsync("Username");
                await _localstorage.RemoveItemAsync("Role");
                await _localstorage.RemoveItemAsync("Id");
                await _localstorage.RemoveItemAsync("HotelId");

                NavigationManager?.NavigateTo("/", forceLoad: true);
            }
        }

        public string GetPrimaryColor(string type)
        {
            return $"{type}: {HotelColor};";
        }

    }
}