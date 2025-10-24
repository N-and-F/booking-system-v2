using Microsoft.AspNetCore.Components;
using reservationSystem.Models;
using reservationSystem.Models.DTO;
using reservationSystem.Data;
using reservationSystem.BusinessLogic;
using MudBlazor;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;
using reservationSystem.Components.Dialogs;

namespace reservationSystem.Components.Pages
{
    public partial class Dashboard : ComponentBase
    {
        [Inject]
        public DataSet? DataSet { get; set; } // Injecting your DbContext

        public List<Booking> BookingList { get; set; } = new List<Booking>();
        [Inject] private BookingsLogic BookingsLogic { get; set; }
        [Inject] private GuestsLogic GuestsLogic { get; set; }
        [Inject] private ReportsLogic ReportsLogic { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        public List<TodayBookingDTO> CheckInBookingList { get; set; } = [];
        public List<TodayBookingDTO> CheckOutBookingList { get; set; } = [];
         
        public MostBookedRoomDTO MostBookedRoom { get; private set; }
        public int TotalBookings { get; set; }
        public int TotalDaysOfBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueUSD { get; set; }
        public int DateSelection { get; set; } = 0;
        public double PercentageOfBookedRooms { get; set; }
        public int TotalRoomsBooked { get; set; } = 0;
        public double TotalBookingsDiffPercentage { get; set; } = 0;


        private ChartOptions OptionsRevenue = new ChartOptions();
        private ChartOptions OptionsReservation = new ChartOptions();
        public List<ChartSeries> ReservationSeries = new();
        public List<ChartSeries> RevenueSeries = new();

        public string[] XAxisLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        private int Index = -1;
        public bool IsLoading { get; set; } = false;
        public bool IsSecondaryLoading { get; set; } = false;
        public int Role { get; set; }
        public string HotelColor { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // Fetch data from DbContext
            await Initialize();
        }

        private async Task Initialize()
        {
            IsLoading = true;
            IsSecondaryLoading = true;

            Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
            HotelColor = await _localstorage.GetItemAsStringAsync("HotelColor");
            HotelColor = HotelColor.Replace('\"', ' ').Trim();

            BookingList = await BookingsLogic.GetAllBookings();
            CheckInBookingList = await BookingsLogic.GetTodayBookings(true);
            CheckOutBookingList = await BookingsLogic.GetTodayBookings(false);
            await GetSecondaryData();

            GetLineChartData(DateTime.Now.Year);
            GetLineChartData(DateTime.Now.Year - 1);

            OptionsRevenue.InterpolationOption = InterpolationOption.Straight;
            OptionsRevenue.YAxisTicks = 50000;

            OptionsReservation.InterpolationOption = InterpolationOption.Straight;
            OptionsReservation.YAxisTicks = 10;
            OptionsReservation.ChartPalette = [HotelColor,"#b3bfd1"];
            IsLoading = false;

        }

        private async Task GetSecondaryData(int selection = 2)
        {
            IsSecondaryLoading = true;
            DateSelection = selection;
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now;
            DateTime prevStartDate = DateTime.Now;
            DateTime prevEndDate = DateTime.Now;


            if (selection == 0) // this week
            {
                DayOfWeek currentDay = DateTime.Now.DayOfWeek;
                int daysTillCurrentDay = currentDay - DayOfWeek.Sunday;
                int daysPastCurrentDay = DayOfWeek.Saturday - currentDay;
                startDate = startDate.AddDays(-daysTillCurrentDay);
                endDate = endDate.AddDays(daysPastCurrentDay + 1);
                prevStartDate = startDate.AddDays(-7);
                prevEndDate = endDate.AddDays(-7);
            }
            else if (selection == 1) // this month
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                endDate = startDate.AddMonths(1).AddDays(-1);
                prevStartDate = startDate.AddMonths(-1);
                prevEndDate = prevStartDate.AddMonths(1).AddDays(-1);

            }
            else if (selection == 2) // this year
            {
                startDate = new DateTime(DateTime.Now.Year, 1, 1);
                endDate = new DateTime(DateTime.Now.Year, 12, 31);
                prevStartDate = startDate.AddYears(-1);
                prevEndDate = prevStartDate.AddYears(1).AddDays(-1);
            }
            else if (selection == 4) // last month
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                startDate = startDate.AddMonths(-1);
                endDate = startDate.AddMonths(1).AddDays(-1);
                prevStartDate = startDate.AddMonths(-1);
                prevEndDate = prevStartDate.AddMonths(1).AddDays(-1);
            }
            else if (selection == 3)
            {
                startDate = DateTime.MinValue;
                endDate = DateTime.MaxValue;
            }

            startDate = startDate.Date;
            endDate = endDate.Date;

            var bookings = BookingsLogic.GetOverlappingBookings(startDate, endDate, BookingList);
            TotalRoomsBooked = BookingsLogic.GetNumberOfRoomBookings(startDate, endDate, bookings);
            MostBookedRoom = BookingsLogic.GetMostBookedRoom(startDate, endDate, bookings);
            (TotalBookings, TotalDaysOfBookings) = BookingsLogic.GetTotalBookings(startDate, endDate, BookingList);
            (int prevTotalBookings, int prevTotalDaysOfBookings) = BookingsLogic.GetTotalBookings(prevStartDate, prevEndDate, BookingList);
            if(prevTotalBookings != 0)
            {
                TotalBookingsDiffPercentage = Math.Round((double)(TotalBookings - prevTotalBookings) / prevTotalBookings * 100, 2);
            }
            else
            {
                TotalBookingsDiffPercentage = (double)TotalBookings;
            }
            
            
            TotalRevenue = BookingsLogic.CalculateTotalRevenueInPeso(startDate, endDate, BookingList);
            RevenueUSD = await BookingsLogic.ConvertRevenueToUSD(TotalRevenue);
            

            PercentageOfBookedRooms = BookingsLogic.CalculateBookingPercentage(startDate, endDate, TotalRoomsBooked);

            IsSecondaryLoading = false;

            
        }

        private void GetLineChartData(int year)
        {
            double[] reservationPerMonth = new double[12];

            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                (int bookingsCount, int x) = BookingsLogic.GetTotalBookings(startDate, endDate, BookingList);

                reservationPerMonth[month - 1] = bookingsCount;
            }

            ReservationSeries.Add(new ChartSeries
            {
                Name = year.ToString(),
                Data = reservationPerMonth
                
            });
        }

        public static string FormatDecimalWithCommas(decimal number)
        {
            // Format the number with commas every three digits.
            string formattedNumber = number.ToString("#,##0");

            // Manually insert a comma and space every 4 digits.
            // This is a simple approach and might not be perfect for all cases, especially for very large numbers.
            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (char c in formattedNumber)
            {
                if (c == ',')
                {
                    count = 0; // Reset the count after a comma.
                }
                else if (count == 4)
                {
                    sb.Append(", "); // Insert a comma and space after every 4 digits.
                    count = 1; // Start counting again.
                }
                else
                {
                    count++;
                }
                sb.Append(c);
            }

            return sb.ToString();
        }

        public string FormatDateSelection(int dateSelected)
        {
            switch (dateSelected)
            {
                case 0:
                    return "vs last week";
                case 1:
                    return "vs last month";
                case 2:
                    return "vs last year";
                case 4:
                    var twoMonthsAgo = DateTime.Now.AddMonths(-2);
                    return $"(last month vs {twoMonthsAgo:MMMM})";
                default:
                    return "n/a";
            }
        }

        public async Task HandleExportData()
        {
            var parameters = new DialogParameters<ReportExportDialog>
            {
            };

            var dialog = await DialogService.ShowAsync<ReportExportDialog>(null, parameters);
            var result = await dialog.Result;
        }

        public bool HasManagerAccess()
        {
            if (Role <= (int)Enums.RoleTypes.Manager) return true;
            return false;
        }

        public bool HasAdminAccess()
        {
            if (Role <= (int)Enums.RoleTypes.Admin) return true;
            return false;
        }

        private async Task ShowNotes(string notes)
        {
            var parameters = new DialogParameters<NotesDialog>
            {
                { x => x.Notes, notes }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            await DialogService.ShowAsync<NotesDialog>("Booking Notes", parameters, options);
        }

    }
}

