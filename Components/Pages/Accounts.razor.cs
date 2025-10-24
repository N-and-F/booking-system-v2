using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using reservationSystem.BusinessLogic;
using reservationSystem.Components.Dialogs;
using reservationSystem.Models;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace reservationSystem.Components.Pages
{
    public partial class Accounts
    {

        public List<Account> AccountList { get; set; } = new List<Account>();
        [Inject] private AccountsLogic AccountsLogic { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ILocalStorageService _localstorage { get; set; }
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
            
            AccountList = await AccountsLogic.GetTableItems();
            Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
            HotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            HotelColor = (await _localstorage.GetItemAsStringAsync("HotelColor")).Replace('\"', ' ').Trim();
            if (Role == (int)Enums.RoleTypes.Staff)
            {
                IsStaff = true;
            }
            IsLoading = false;
        }

        //public void OnSearchFilterChanged(string filter)
        //{
        //    SearchFilter = filter;
        //    FilteredRoomList = string.IsNullOrEmpty(SearchFilter)
        //        ? RoomList
        //        : RoomsLogic.GetTableItemsFiltered(RoomList, SearchFilter);

        //}

        private async Task HandleSelectedItemChanged(Account account, string type)
        {
            var parameters = new DialogParameters<AccountsDialog>
            {
                { x => x.Account, account },
                { x => x.DialogType, type },
                { x => x.AccountList, AccountList}
            };

            var dialogOptions = new DialogOptions() { CloseButton = true };

            var dialog = await DialogService.ShowAsync<AccountsDialog>(null, parameters, dialogOptions);
            var result = await dialog.Result;

            if (!result.Canceled || type == "View")
            {
                AccountList = [];
                await Initialize();
                StateHasChanged();
            }
        }

        public static string FormatDecimal(decimal value)
        {
            return value.ToString("N2", new System.Globalization.CultureInfo("en-US"));
        }


    }
}