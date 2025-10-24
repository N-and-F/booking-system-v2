using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Extensions;
using MudBlazor;
using reservationSystem.BusinessLogic;
using reservationSystem.Components.Dialogs;
using reservationSystem.Models;
using System.ComponentModel;


namespace reservationSystem.Components.Pages
{
    public partial class Bookings
    {

        public List<Booking> BookingList { get; set; } = new List<Booking>();
        public List<Booking> FilteredBookingList = new List<Booking>();
        [Inject] private BookingsLogic BookingsLogic { get; set; }
        [Inject] private GuestsLogic GuestsLogic { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        public bool IsLoading { get; set; } = false;
        public string HotelColor { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public List<Guest> GuestList { get; set; }

        public string SearchFilter { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            await Initialize();
        }

        private async Task Initialize(bool isActiveBooking = true)
        {
            IsLoading = true;
            HotelColor = (await _localstorage.GetItemAsStringAsync("HotelColor")).Replace('\"', ' ').Trim();
            BookingList = await BookingsLogic.GetTableItems(isActiveBooking);
            FilteredBookingList = new List<Booking>(BookingList);
            GuestList = await GuestsLogic.GetTableItems();
            IsActiveBookings = isActiveBooking;
            IsLoading = false;
        }

        public void OnSearchFilterChanged(string filter)
        {
            SearchFilter = filter;
            FilteredBookingList = string.IsNullOrEmpty(SearchFilter)
                ? BookingList
                : BookingsLogic.GetTableItemsFiltered(BookingList, GuestList, SearchFilter);

        }

        public bool IsActiveBookings { get; set; } = true;
        private async Task HandleAddButton()
        {

            Guid id = Guid.NewGuid();
            NavigationManager.NavigateTo("/bookings/" + id);
        }

        public async Task HandleDeleteButton(Booking booking)
        {
            var parameters = new DialogParameters<BookingDialog>
            {
                { x => x.Booking, booking },
            };

            var dialog = await DialogService.ShowAsync<BookingDialog>(null, parameters);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize();
            }
        }

        public async Task HandleCheckAvailabilityButton()
        {
            var parameters = new DialogParameters<AvailabilityCheckDialog>
            {};

            var dialog = await DialogService.ShowAsync<AvailabilityCheckDialog>(null, parameters);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize(IsActiveBookings);
            }
        }

        public async Task HandleUpdatePaymentButton(Booking booking)
        {
            var parameters = new DialogParameters<PaymentDialog>
            {
                { x => x.Booking, booking },
            };

            var dialog = await DialogService.ShowAsync<PaymentDialog>(null, parameters);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize(IsActiveBookings);
            }
        }

        

        private string GetGuestName(int id)
        {
            var guest = GuestList.Where(g => g.Id == id).FirstOrDefault();

            return guest != null ? guest.Name : string.Empty;
        }

        private string CalculateBalance(Booking booking)
        {
            var bal = booking.Total - booking.Paid;
            return AccountsLogic.FormatMoney(bal);

        }

        public async Task ShowConfirmation(Booking booking)
        {
            var parameters = new DialogParameters<ConfirmedBookingDialog>
            {
                { x => x.BookingID, booking.Id },
                { x => x.IsEditable, true },
            };

            var dialog = await DialogService.ShowAsync<ConfirmedBookingDialog>(null, parameters);

            var result = await dialog.Result;
        }

        public (string Color, string Label) GetGuestStatus(int guestId)
        {
            var guest = GuestList.FirstOrDefault(g => g.Id == guestId);
            if (guest == null) return (string.Empty, string.Empty);

            return guest.NumBookings switch
            {
                1 => ("#296E01", "New"),
                > 1 and < 5 => ("#004999", "Regular"),
                >= 5 => ("#D3AF37", "Suki"),
                _ => (string.Empty, string.Empty)
            };
        }
    }
}