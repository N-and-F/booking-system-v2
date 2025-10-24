using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using MudBlazor;
using reservationSystem.BusinessLogic;
using reservationSystem.Components.Dialogs;
using reservationSystem.Models;
using reservationSystem.Models.DTO;
using System.ComponentModel.DataAnnotations;
using static Azure.Core.HttpHeader;
using static MudBlazor.Colors;

namespace reservationSystem.Components.Pages
{
    public partial class BookingForm
    {
        [Parameter] public string Id { get; set; }
        [Inject] private BookingsLogic BookingsLogic { get; set; }
        [Inject] private GuestsLogic GuestsLogic { get; set; }
        [Inject] private UsersLogic UsersLogic { get; set; }
        [Inject] private RoomsLogic RoomsLogic { get; set; }
        [Inject] private AccountsLogic AccountsLogic { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        BookingDTO? CurrentBooking { get; set; } // to populate the fields
        private bool IsLoading { get; set; } = true;
        public List<Guest> GuestList { get; set; }
        public Guest SelectedGuest { get; set; } = new Guest();
        public List<Room> RoomList { get; set; } = new List<Room>();
        public List<string> Countries { get; set; } = new List<string>();
        public List<Account> AccountList { get; set; } = new();
        public int SelectedAccount { get; set; } = -1;
        public BookingDTO PrevData { get; set; }
        
        private IEnumerable<int> _selectedRooms = new List<int>();
        public IEnumerable<int> SelectedRooms
        {
            get => _selectedRooms;
            set
            {
                _selectedRooms = value;
                CalculatePayment();
            }
        }
        public string Name { get; set; }
        private DateTime? _checkIn;
        public DateTime? CheckIn 
        {
            get => _checkIn;
            set
            {
                _checkIn = value;
                CalculatePayment();
            } 
        }
        private DateTime? _checkOut;
        public DateTime? CheckOut
        {
            get => _checkOut;
            set
            {
                _checkOut = value;
                CalculatePayment();
            }
        }
        private bool IsCheckInDisabled { get; set; } = false;
        private bool IsCheckOutDisabled { get; set; } = false;
        public string? Notes { get; set; } = "";
        public string RoomPayment { get; set; } = "0";
        private decimal _additionalPayment;

        public decimal AdditionalPayment
        {
            get => _additionalPayment;
            set
            {
                _additionalPayment = value;
                CalculatePayment();
            }
        }
        private decimal _deduction;

        public decimal Deduction
        {
            get => _deduction;
            set
            {
                _deduction = value;
                CalculatePayment();
            }
        }
        public decimal TotalPayment { get; set; } = 0;
        public decimal Paid { get; set; }
        public string Created { get; set; } = "";
        public string Updated { get; set; } = "";

        private bool resetValueOnEmptyText;
        Func<int, string>? ToStringConverter;

        protected override async Task OnInitializedAsync()
        {
            await Initialize();
        }
        private async Task Initialize()
        {
            try
            {
                IsLoading = true;
                var users = await UsersLogic.GetTableItems();
                CurrentBooking = await BookingsLogic.GetBookingDetails(Id);
                SelectedGuest = await GuestsLogic.GetGuestDetails(CurrentBooking.GuestId);
                RoomList = await RoomsLogic.GetTableItems();
                GuestList = await GuestsLogic.GetTableItems();
                AccountList = await AccountsLogic.GetTableItems();
                if (AccountList.Any(x => x.Id == CurrentBooking.AccountId))
                {
                    SelectedAccount = AccountList.FirstOrDefault(x => x.Id == CurrentBooking.AccountId).Id;
                }
                Countries = BookingsLogic.CountryList.Distinct().ToList();
                Countries.Sort();

                IsLoading = false;

                Name = SelectedGuest.Name;
                CheckIn = CurrentBooking.StartDate;
                CheckOut = CurrentBooking.EndDate;
                SelectedRooms = CurrentBooking.RoomIds;
                Notes = CurrentBooking.Notes;
                TotalPayment = CurrentBooking.Total;
                Paid = CurrentBooking.Paid;
                AdditionalPayment = CurrentBooking.AddOns;
                Deduction = CurrentBooking.Deductions;
                PrevData = new BookingDTO(CurrentBooking);
                ToStringConverter = DisplayRoomName;


                if (CurrentBooking.CreatedOn != null && CurrentBooking.CreatedBy != null)
                {
                    var createdBy = users.Where(x => x.Id == CurrentBooking.CreatedBy).FirstOrDefault();
                    var createdOn = CurrentBooking.CreatedOn?.ToString("MMM dd, yyyy hh:mm tt");
                    Created = $"Booked By: {createdBy.Username} ({createdOn})";
                }
                if (CurrentBooking.UpdatedOn != null && CurrentBooking.UpdatedBy != null)
                {
                    var updatedBy = users.Where(x => x.Id == CurrentBooking.UpdatedBy).FirstOrDefault();
                    var updatedOn = CurrentBooking.UpdatedOn?.ToString("MMM dd, yyyy hh:mm tt");
                    Updated = $"Updated By: {updatedBy.Username} ({updatedOn})";
                }



                IsCheckInDisabled = CheckIn < DateTime.Now.Date && CheckOut > DateTime.Now.Date;
                //IsCheckOutDisabled = CheckOut < DateTime.Now.Date;
            }
            catch
            {
                Snackbar.Add("One or more errors occurred.", Severity.Error);
                IsLoading = false;
                
                NavigationManager.NavigateTo("/bookings");
            }
            
            
        }
        private void HandleBack()
        {
            NavigationManager.NavigateTo("/bookings");
        }
        private async Task HandleSave()
        {
            IsLoading = true;

            if (!ValidateDates())
            {
                Snackbar.Add("Invalid Dates", Severity.Error);
                IsLoading = false;
                return;
            }

            if (!ValidateGuestInformation())
            {
                Snackbar.Add("Guest information should not be empty", Severity.Error);
                IsLoading = false;
                return;
            }

            if (!ValidateRoomsSelection())
            {
                Snackbar.Add("Please choose at least one room", Severity.Error);
                IsLoading = false;
                return;
            }

            if (SelectedAccount == -1)
            {
                Snackbar.Add("Please choose an account", Severity.Error);
                IsLoading = false;
                return;
            }

            var unavailableRoom = await BookingsLogic.IsDatesAvailable(Id, (DateTime)CheckIn, (DateTime)CheckOut, SelectedRooms.ToList());
            if (unavailableRoom != 0)
            {
                var room = RoomList.Find(x => x.Id == unavailableRoom);
                Snackbar.Add($"{room.Name} is unavailable", Severity.Error);
                IsLoading = false;
                return;
            }

            int id = await GuestsLogic.SaveGuest(SelectedGuest.Name, SelectedGuest.Email, SelectedGuest.Country);
            if (id == -1 || CheckIn == null || CheckOut == null)
            {
                Snackbar.Add($"Something went wrong", Severity.Error);
                IsLoading = false;
                return;
            }

            await SaveBooking(id);
            IsLoading = false;
            await ShowConfirmation();
            NavigationManager.NavigateTo("/bookings");
        }

        private bool ValidateDates()
        {
            return CheckIn != null && CheckOut != null && CheckOut >= CheckIn;
        }

        private bool ValidateGuestInformation()
        {
            return !Name.IsNullOrEmpty() && !SelectedGuest.Email.IsNullOrEmpty() && !SelectedGuest.Country.IsNullOrEmpty();
        }

        private bool ValidateRoomsSelection()
        {
            return SelectedRooms.Any();
        }

        private async Task SaveBooking(int guestId)
        {
            CurrentBooking.GuestId = guestId;
            CurrentBooking.StartDate = (DateTime)CheckIn;
            CurrentBooking.EndDate = (DateTime)CheckOut;
            CurrentBooking.RoomIds = SelectedRooms.ToList();
            CurrentBooking.Notes = Notes;
            CurrentBooking.Total = TotalPayment;
            CurrentBooking.Paid = Paid;
            CurrentBooking.AddOns = AdditionalPayment;
            CurrentBooking.Deductions = Deduction;
            CurrentBooking.AccountId = SelectedAccount;
            CurrentBooking.Account = AccountList.FirstOrDefault(x => x.Id == SelectedAccount);
            await BookingsLogic.HandleSaveBooking(CurrentBooking, PrevData);
        }

        public async Task ShowConfirmation()
        {
            var parameters = new DialogParameters<ConfirmedBookingDialog>
            {
                { x => x.BookingID, Guid.Parse(Id) },
                { x => x.IsEditable, false},
            };

            var dialog = await DialogService.ShowAsync<ConfirmedBookingDialog>(null, parameters);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize();
            }
        }


        // Guest Information Specific
        private async Task<IEnumerable<string>> SearchGuest(string value)
        {
            var guests = await GuestsLogic.GetTableItems(value);
            return guests.Select(g => g.Name).Distinct();
        }

        private async void GuestValueChanged(string value)
        {
            Name = value;
            var guest = GuestList.Where(g => g.Name == Name).FirstOrDefault();
            if(guest != null) 
            {
                SelectedGuest = new Guest()
                {
                    Name = guest.Name,
                    Email = guest.Email,
                    Country = guest.Country,
                };
            }
            else
            {
                SelectedGuest.Name = Name;
                SelectedGuest.Email = "";
                SelectedGuest.Country = "Philippines";
            }
            
        }


        // Booking Details Specific
        private string DisplayRoomName(int roomId)
        {
            return RoomList?.FirstOrDefault(x => x.Id == roomId)?.Name ?? string.Empty;
        }
        private void CalculatePayment()
        {
            if (CheckIn >= CheckOut)
            {
                CheckOut = CheckIn.Value.AddDays(1);
            }
            
            if (!SelectedRooms.Any())
            {
                RoomPayment = "0";
                TotalPayment = 0;
                return;
            }

            int? numDays = null;

            if (CheckIn.HasValue && CheckOut.HasValue)
            {
                // Ensure both CheckIn and CheckOut have values
                var difference = CheckOut.Value.Date - CheckIn.Value.Date;
                numDays = difference.Days;
            }
            var roomPayment = SelectedRooms.Sum(roomId =>
            {
                var room = RoomList.FirstOrDefault(r => r.Id == roomId);
                return room?.Price * numDays ?? 0;
            });

            RoomPayment = roomPayment.ToString();

            TotalPayment = (roomPayment + AdditionalPayment) - Deduction;
        }

    }
}