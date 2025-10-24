using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using reservationSystem.BusinessLogic;
using reservationSystem.Components.Dialogs;
using reservationSystem.Models;
using System.ComponentModel;
using System.Data;

namespace reservationSystem.Components.Pages
{
    public partial class Rooms
    {

        public List<Room> RoomList { get; set; } = new List<Room>();
        public List<Room> FilteredRoomList = new List<Room>();
        [Inject]
        private RoomsLogic RoomsLogic { get; set; }
        [Inject]
        private IDialogService DialogService { get; set; }
        [Inject]
        private ILocalStorageService _localstorage { get; set; }
        public bool IsLoading { get; set; } = false;
        public int Role { get; set; } = 2;
        public bool IsStaff { get; set; } = false;
        public int HotelId { get; set; }
        public string HotelColor { get; set; }
        public List<RoomType> RoomTypes { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public string SearchFilter { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            await Initialize();
        }

        private async Task Initialize()
        {
            IsLoading = true;
            RoomList = await RoomsLogic.GetTableItems();
            FilteredRoomList = new List<Room>(RoomList);
            RoomTypes = await RoomsLogic.GetRoomTypes();
            Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
            HotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            HotelColor = (await _localstorage.GetItemAsStringAsync("HotelColor")).Replace('\"', ' ').Trim();
            if (Role == (int)Enums.RoleTypes.Staff)
            {
                IsStaff = true;
            }
            IsLoading = false;
        }


        public void OnSearchFilterChanged(string filter)
        {
            SearchFilter = filter;
            FilteredRoomList = string.IsNullOrEmpty(SearchFilter)
                ? RoomList
                : RoomsLogic.GetTableItemsFiltered(RoomList, SearchFilter);

        }

        private async Task HandleManageButton()
        {
            var parameters = new DialogParameters<ManageRoomTypesDialog>
            {};

            var dialog = await DialogService.ShowAsync<ManageRoomTypesDialog>(null, parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize();
            }
        }

        private async Task HandleSelectedItemChanged(Room room, string type)
        {
            var parameters = new DialogParameters<RoomDialog>
            {
                { x => x.Room, room },
                { x => x.DialogType, type },
                { x => x.IsStaff, IsStaff}
            };

            var dialog = await DialogService.ShowAsync<RoomDialog>(null, parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize();
            }
        }
    }
}