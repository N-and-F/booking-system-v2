using reservationSystem.Models;
using reservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace reservationSystem.BusinessLogic
{
    public class ReportsLogic
    {
        private readonly reservationSystem.Data.DataSet _context;
        [Inject] public ILocalStorageService _localstorage { get; set; }
        public IJSRuntime _jsRuntime { get; set; }
        public ReportsLogic(IJSRuntime jsRuntime, reservationSystem.Data.DataSet context, ILocalStorageService LocalStorage)
        {
            _context = context;
            _localstorage = LocalStorage;
            _jsRuntime = jsRuntime;
        }

        public async Task ExportData(DateTime startDate, DateTime endDate)
        {
            var path = "Monthly_Report.pdf";
            var data = await PrepareData(startDate, endDate);
            GeneratePDF(data, path);
            // Convert PDF to base64 string
            var bytes = File.ReadAllBytes(path);
            var base64Data = Convert.ToBase64String(bytes);

            // Invoke JavaScript to download
            await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", base64Data, path);
        }

        public async Task<List<DailyReport>> PrepareData(DateTime startDate, DateTime endDate)
        {
            startDate = startDate.Date;
            endDate = endDate.Date;
            var hotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));

            var bookings = await _context.Bookings.Where(x => x.StartDate.Date <= endDate && 
                                                              startDate <= x.EndDate.Date &&
                                                              x.HotelId == hotelId)
                                                  .Include(x => x.BookingRooms)
                                                  .ThenInclude(x => x.Room)
                                                  .ToListAsync();
            var rooms = await _context.Rooms.Where(x => x.OriginalId == null && x.HotelId == hotelId).ToListAsync();
            List<DailyReport> report = new List<DailyReport>();

            var currentDate = startDate;
            for ( var i = 0; currentDate < endDate; i++ )
            {
                currentDate = startDate.AddDays(i);
                var dailyReport = new DailyReport
                {
                    Date = currentDate, // Include the date in the report
                    NumGuestCheckIn = bookings
                        .Where(b => b.StartDate == currentDate)
                        .SelectMany(b => b.BookingRooms)
                        .Sum(br => br.Room.NumGuests),
                    NumGuestOvernight = bookings
                        .Where(b => b.StartDate <= currentDate && currentDate < b.EndDate)
                        .SelectMany(b => b.BookingRooms)
                        .Sum(br => br.Room.NumGuests),
                    NumRoomsOccupied = 0,
                    RoomGuestCount = rooms.ToDictionary(r => r.Name, r => 0) // Initialize room counts
                };
                var roomGuestCount = rooms.ToDictionary(r => r.Id, r => 0);

                var currentBookings = bookings.Where(x => currentDate >= x.StartDate && currentDate < x.EndDate).ToList();

                foreach (var booking in currentBookings)
                {
                    foreach (var bookingRoom in booking.BookingRooms)
                    {
                        if (roomGuestCount.ContainsKey(bookingRoom.RoomId))
                        {
                            roomGuestCount[bookingRoom.RoomId] += bookingRoom.Room.NumGuests;
                        }
                        else
                        {
                            var id = bookingRoom.Room.OriginalId ?? -1;
                            if (id != -1)
                            {
                                roomGuestCount[id] += bookingRoom.Room.NumGuests;
                            }
                            else
                            {
                                Console.WriteLine("Something went wrong");
                            }
                            
                        }   
                    }
                }
                dailyReport.NumRoomsOccupied = roomGuestCount.Where(x => x.Value > 0).Count();

                foreach (var key in roomGuestCount.Keys)
                {
                    var room = rooms.FirstOrDefault(x => x.Id == key);
                    if (room != null)
                    {
                        dailyReport.RoomGuestCount[room.Name] = roomGuestCount[key];
                    }
                    else
                    {
                        Console.WriteLine("Something went wrong");
                    }
                    
                }

                report.Add(dailyReport);
            }

            return report;
        }

        public void GeneratePDF(List<DailyReport> reports, string reportName)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header()
                        .Text("Monthly Report")
                        .SemiBold().FontSize(36);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            IContainer DefaultCellStyle(IContainer container, string backgroundColor)
                            {
                                return container
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Background(backgroundColor)
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(10)
                                    .AlignCenter()
                                    .AlignMiddle();
                            }
                            // 1. Get all unique room numbers to define columns
                            var allRoomNumbers = new HashSet<string>();
                            foreach (var report in reports)
                            {
                                allRoomNumbers.UnionWith(report.RoomGuestCount.Keys);
                            }

                            // 2. Define standard columns (Date, CheckIn, Overnight, Occupied)
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(100);  // Date
                                foreach (var roomNumber in allRoomNumbers)
                                {
                                    columns.RelativeColumn(); // One column for each room
                                }
                                columns.ConstantColumn(80); // CheckIn
                                columns.ConstantColumn(80);  // Overnight
                                columns.ConstantColumn(80);  // Occupied
                            });

                            // 3. Header Row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date");
                                foreach (var roomNumber in allRoomNumbers)
                                {
                                    header.Cell().Element(CellStyle).Text($"{roomNumber}");
                                }
                                header.Cell().Element(CellStyle).Text("Check-in Guests");
                                header.Cell().Element(CellStyle).Text("Overnight Guests");
                                header.Cell().Element(CellStyle).Text("Rooms Occupied");
                                IContainer CellStyle(IContainer container) => DefaultCellStyle(container, Colors.Grey.Lighten3);
                            });

                            // 4. Populate Data Rows
                            foreach (var report in reports)
                            {
                                table.Cell().Element(CellStyle).Text(report.Date.ToString("MM/dd/yyyy"));
                                foreach (var room in report.RoomGuestCount.Keys)
                                {
                                    table.Cell().Element(CellStyle).Text(report.RoomGuestCount[room].ToString());
                                }
                                table.Cell().Element(CellStyle).Text(report.NumGuestCheckIn.ToString());
                                table.Cell().Element(CellStyle).Text(report.NumGuestOvernight.ToString());
                                table.Cell().Element(CellStyle).Text(report.NumRoomsOccupied.ToString());
                                IContainer CellStyle(IContainer container) => DefaultCellStyle(container, Colors.White).ShowOnce();
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            })
            .GeneratePdf(reportName);
        }

    }
}
