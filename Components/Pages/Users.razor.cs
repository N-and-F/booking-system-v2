using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.JSInterop;
using MudBlazor;
using reservationSystem.BusinessLogic;
using reservationSystem.Components.Dialogs;
using reservationSystem.Models;
using System.ComponentModel;

namespace reservationSystem.Components.Pages
{
    public partial class Users
    {

        public List<User> UserList { get; set; } = new List<User>();
        public List<User> FilteredUserList = new List<User>();
        [Inject]
        private UsersLogic UsersLogic { get; set; }
        [Inject]
        private IDialogService DialogService { get; set; }
        [Inject]
        private ILocalStorageService _localstorage { get; set; }
        [Inject]
        private NavigationManager? NavigationManager { get; set; }
        public bool IsLoading { get; set; } = false;
        public int Role { get; set; } = 2;
        public string HotelColor { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public string SearchFilter { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            await Initialize();
        }

        private async Task Initialize()
        {
            IsLoading = true;
            UserList = await UsersLogic.GetTableItems();
            FilteredUserList = new List<User>(UserList);
            Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
            HotelColor = (await _localstorage.GetItemAsStringAsync("HotelColor")).Replace('\"', ' ').Trim();
            if (Role > (int)Enums.RoleTypes.Admin)
            {
                NavigationManager?.NavigateTo("/", forceLoad: true);
            }
            IsLoading = false;
        }

        public string MapRole(int role)
        {
            return UsersLogic.MapRole(role);
        }

        public string FormatDate(DateTime? date) 
        {
            return date?.ToString("MMMM dd, yyyy hh:mm tt") ?? "";
        }


        public void OnSearchFilterChanged(string filter)
        {
            SearchFilter = filter;
            FilteredUserList = string.IsNullOrEmpty(SearchFilter)
                ? UserList
                : UsersLogic.GetTableItemsFiltered(UserList, SearchFilter);

        }

        private async Task HandleSelectedItemChanged(User user, string type)
        {
            var parameters = new DialogParameters<UserDialog>
            {
                { x => x.User, user },
                { x => x.DialogType, type }
            };

            var dialog = await DialogService.ShowAsync<UserDialog>(null, parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize();
            }
        }
    }
}