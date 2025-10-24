using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using reservationSystem.BusinessLogic;
using reservationSystem.Models;
using System.ComponentModel;
using reservationSystem.Enums;
using reservationSystem.Models.DTO;

namespace reservationSystem.Components.Pages
{
    public partial class Reports
    {

        public List<Account> AccountList { get; set; } = new List<Account>();
        [Inject] private AccountsLogic AccountsLogic { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ILocalStorageService _localstorage { get; set; }
        public bool IsLoading { get; set; } = false;
        public int Role { get; set; } = 2;
        public int HotelId { get; set; }
        public string HotelColor { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public string[] ProfitChartLabels { get; set; } = ["Revenue", "Expenses"];
        public double[] ProfitChartData { get; set; } = [0, 0];
        public double[] ExpensesChartData { get; set; } = [0, 0, 0, 0, 0, 0, 0];
        public double[] IncomeChartData { get; set; } = [0, 0, 0, 0, 0, 0, 0, 0];
        public Dictionary<string, double[]> SummaryTableData { get; set; } = new Dictionary<string, double[]>();
        public List<ReportsTable> TableData { get; set; } = new List<ReportsTable>();

        public string Profit { get; set; }
        public string ProfitGoal { get; set; } = "Goal: ";

        public string ProfitCategory { get; set; }
        public int ProfitChartIndex { get; set; } = -1;

        private MudDateRangePicker _picker;
        private DateRange _dateRange = new DateRange(DateTime.Now.AddDays(-31).Date, DateTime.Now.Date);
        private DateTime _maxDate = DateTime.Now.Date;
        private DateTime _minDate = new DateTime(2024, 08, 1);

        public string[] BarChartXAxis { get; set; } = [];
        public List<AccountTransaction> Transactions { get; set; }
        public List<ChartSeries> BarSeries = [];
        public ChartOptions BarChartOptions = new ChartOptions();
        public ChartOptions ProfitChartOptions = new ChartOptions();
        public ChartOptions ExpenseChartOptions = new ChartOptions();
        public ChartOptions IncomeChartOptions = new ChartOptions();

        public string[] ExpenseLabels = ["Hotel",
                                         "Store",
                                         "GCash", 
                                         "Load",
                                         "Van",
                                         "Motor",
                                         "Other"];
        public string[] IncomeLabels = ["Hotel",
                                         "Store",
                                         "GCash In",
                                         "Gcash Out",
                                         "Load",
                                         "Van",
                                         "Motor",
                                         "Other"];


        protected override async Task OnInitializedAsync()
        {
            await Initialize();
        }

        private async Task Initialize()
        {
            IsLoading = true;
            Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
            HotelId = int.Parse(await _localstorage.GetItemAsStringAsync("HotelId"));
            HotelColor = (await _localstorage.GetItemAsStringAsync("HotelColor")).Replace('\"', ' ').Trim();
            Transactions = await AccountsLogic.GetAllTransactions(startDate: _dateRange.Start, endDate: _dateRange.End);
            var dateRangeLabel = GetDateRangeLabel(_dateRange);
            BarChartXAxis = CalculateXAxisLabels(dateRangeLabel);
            BarSeries = GetChartSeries(dateRangeLabel);
            GetExpenseIncomeChartSeries();
            BarChartOptions.YAxisTicks = 5000;
            BarChartOptions.MaxNumYAxisTicks = 7;
            BarChartOptions.YAxisFormat = "₱ #,##0";
            BarChartOptions.ChartPalette = ["#00bfa0","#e60049"];
            ProfitChartOptions.ChartPalette = ["#00bfa0", "#e60049"];
            ExpenseChartOptions.ChartPalette = [
               "#fd7f6f",
               "#7eb0d5",
               "#b2e061",
               "#ffb55a",
               "#ffee65",
               "#beb9db",
               "#fdcce5",
               "#8bd3c7"
            ];
            IncomeChartOptions.ChartPalette = [
               "#fd7f6f", 
               "#7eb0d5", 
               "#b2e061",
               "#bd7ebe",
               "#ffb55a", 
               "#ffee65",
               "#beb9db", 
               "#fdcce5",
            ];
            TransformTableData();
            IsLoading = false;
        }

        private async Task HandleDateRangeChange(DateRange newDateRange)
        {
            _dateRange = newDateRange;
            await Initialize();
        }
        private static int GetDateRangeLabel(DateRange range)
        {
            if (range.Start >= range.End) return -1;
            var diff = range.End - range.Start;
            if(diff == null) return -1;
            int days = diff.Value.Days;
            if (days <= 14) return (int)DateRangeLabels.Daily;
            else if (days <= 60) return (int)DateRangeLabels.Weekly;
            else if (days <= 365) return (int)DateRangeLabels.Monthly;
            else if (days <= 1095) return (int)DateRangeLabels.Quarterly;
            else return (int)DateRangeLabels.Yearly;
        }
        private string[] CalculateXAxisLabels(int rangeLabel)
        {
            if (rangeLabel == -1) return [];

            List<string> labels = new();

            switch (rangeLabel)
            {
                case (int)DateRangeLabels.Daily:
                    AddDailyLabels(_dateRange, labels);
                    break;
                case (int)DateRangeLabels.Weekly:
                    AddWeeklyLabels(_dateRange, labels);
                    break;
                case (int)DateRangeLabels.Monthly:
                    AddMonthlyLabels(_dateRange, labels);
                    break;
                case (int)DateRangeLabels.Quarterly:
                    AddQuarterlyLabels(_dateRange, labels);
                    break;
                case (int)DateRangeLabels.Yearly:
                    AddYearlyLabels(_dateRange, labels);
                    break;
                default:
                    // Handle unexpected range label
                    break;
            }

            return [.. labels];
        }
        private List<ChartSeries> GetChartSeries (int rangeLabel)
        {
            List<double> revenue = [];
            List<double> expenses = [];

            if (rangeLabel == -1)
            {
                return new List<ChartSeries>();
            }

            DateTime currentDate = (DateTime)_dateRange.Start;
            DateTime endDate = (DateTime)_dateRange.End;
            switch (rangeLabel)
            {
                case (int)DateRangeLabels.Daily:
                    while (currentDate <= endDate)
                    {
                        var currentRevenue = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < currentDate.AddDays(1) &&
                                          IsIncome(t.Type))
                            .Sum(t => t.Amount);

                        var currentExpenses = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < currentDate.AddDays(1) &&
                                         IsExpense(t.Type))
                            .Sum(t => Math.Abs(t.Amount));

                        revenue.Add((double)currentRevenue);
                        expenses.Add((double)currentExpenses);

                        currentDate = currentDate.AddDays(1);
                    }
                    break;
                case (int)DateRangeLabels.Weekly:
                    var currentDay = currentDate.DayOfWeek;
                    int daysTillCurrentDay = currentDay - DayOfWeek.Sunday;
                    currentDate = currentDate.AddDays(-daysTillCurrentDay);
                    while (currentDate <= endDate)
                    {
                        var newEndDate = currentDate.AddDays(7);
                        var currentRevenue = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                          IsIncome(t.Type))
                            .Sum(t => t.Amount);

                        var currentExpenses = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                         IsExpense(t.Type))
                            .Sum(t => Math.Abs(t.Amount));

                        revenue.Add((double)currentRevenue);
                        expenses.Add((double)currentExpenses);

                        currentDate = currentDate.AddDays(7);
                    }
                    break;
                case (int)DateRangeLabels.Monthly:
                    currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                    while (currentDate <= endDate)
                    {
                        var newEndDate = currentDate.AddMonths(1).AddDays(-1);
                        var currentRevenue = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                          IsIncome(t.Type))
                            .Sum(t => t.Amount);

                        var currentExpenses = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                         IsExpense(t.Type))
                            .Sum(t => Math.Abs(t.Amount));

                        revenue.Add((double)currentRevenue);
                        expenses.Add((double)currentExpenses);

                        currentDate = currentDate.AddMonths(1);
                    }
                    break;
                case (int)DateRangeLabels.Quarterly:
                    while (currentDate <= endDate)
                    {
                        var newEndDate = currentDate.AddDays(90);
                        var currentRevenue = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                          IsIncome(t.Type))
                            .Sum(t => t.Amount);

                        var currentExpenses = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                         IsExpense(t.Type))
                            .Sum(t => Math.Abs(t.Amount));

                        revenue.Add((double)currentRevenue);
                        expenses.Add((double)currentExpenses);

                        currentDate = currentDate.AddDays(90);
                    }
                    break;
                case (int)DateRangeLabels.Yearly:
                    currentDate = new DateTime(currentDate.Year, 1, 1);
                    while (currentDate <= endDate)
                    {
                        var newEndDate = currentDate.AddYears(1).AddDays(-1);
                        var currentRevenue = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                          IsIncome(t.Type))
                            .Sum(t => t.Amount);

                        var currentExpenses = Transactions
                            .Where(t => t.Date >= currentDate && t.Date < newEndDate &&
                                         IsExpense(t.Type))
                            .Sum(t => Math.Abs(t.Amount));

                        revenue.Add((double)currentRevenue);
                        expenses.Add((double)currentExpenses);

                        currentDate = currentDate.AddDays(365);
                    }
                    break;
                default:
                    break;
            }
            var total = revenue.Sum() + expenses.Sum();
            ProfitChartData[0] = (revenue.Sum() / total) * 100;
            ProfitChartData[1] = (expenses.Sum() / total) * 100;
            ProfitChartLabels[0] = $"Revenue ({AccountsLogic.FormatMoney((decimal)revenue.Sum())})";
            ProfitChartLabels[1] = $"Expenses ({AccountsLogic.FormatMoney((decimal)expenses.Sum())})";
            Profit = AccountsLogic.FormatMoney((decimal)revenue.Sum() - (decimal)expenses.Sum());
            var goal = 0.15 * revenue.Sum();
            ProfitGoal = "Goal: " + AccountsLogic.FormatMoney((decimal)goal);
            var profitMargin = CalculateProfitMargin(revenue.Sum(), expenses.Sum());
            if (profitMargin < 0) ProfitCategory = "#e60049";
            else if (profitMargin <= 15) ProfitCategory = "#ffb55a";
            else ProfitCategory = "#00bfa0";
            return new List<ChartSeries>
            {
                new ChartSeries { Name = "Revenue", Data = [.. revenue] },
                new ChartSeries { Name = "Expenses", Data = [..expenses] } 
            };
        }
        private void GetExpenseIncomeChartSeries()
        {
            var groupedTransactions = Transactions.GroupBy(x => x.Type);
            var totalExpenses = (double)Transactions.Where(x => IsExpense(x.Type)).Select(x => x.Amount).Sum();
            var totalIncome = (double)Transactions.Where(x => IsIncome(x.Type)).Select(x => x.Amount).Sum();
            SummaryTableData = new Dictionary<string, double[]>();
            foreach (var transaction in groupedTransactions)
            {
                var sum = transaction.Sum(x => (double)x.Amount);
                switch(transaction.Key)
                {
                    case (int)TransactionTypes.HotelExpense:
                        ExpensesChartData[0] = (sum / totalExpenses) * 100;
                        PopulateTableData("Hotel", 1, sum);
                        break;
                    case (int)TransactionTypes.StoreExpense:
                        ExpensesChartData[1] = (sum / totalExpenses) * 100;
                        PopulateTableData("Store", 1, sum);
                        break;
                    case (int)TransactionTypes.GcashExpense:
                        ExpensesChartData[2] = (sum / totalExpenses) * 100;
                        PopulateTableData("Gcash", 1, sum);
                        break;
                    case (int)TransactionTypes.LoadExpense:
                        ExpensesChartData[3] = (sum / totalExpenses) * 100;
                        PopulateTableData("Load", 1, sum);
                        break;
                    case (int)TransactionTypes.VanExpense:
                        ExpensesChartData[4] = (sum / totalExpenses) * 100;
                        PopulateTableData("Van", 1, sum);
                        break;
                    case (int)TransactionTypes.MotorExpense:
                        ExpensesChartData[5] = (sum / totalExpenses) * 100;
                        PopulateTableData("Motor", 1, sum);
                        break;
                    case (int)TransactionTypes.OtherExpense:
                        ExpensesChartData[6] = (sum / totalExpenses) * 100;
                        PopulateTableData("Other", 1, sum);
                        break;
                    case (int)TransactionTypes.HotelIncome:
                        IncomeChartData[0] = (sum / totalIncome) * 100;
                        PopulateTableData("Hotel", 0, sum);
                        break;
                    case (int)TransactionTypes.StoreIncome:
                        IncomeChartData[1] = (sum / totalIncome) * 100;
                        PopulateTableData("Store", 0, sum);
                        break;
                    case (int)TransactionTypes.GcashCashInIncome:
                        IncomeChartData[2] = (sum / totalIncome) * 100;
                        PopulateTableData("Gcash", 0, sum);
                        break;
                    case (int)TransactionTypes.GcashCashOutIncome:
                        IncomeChartData[3] = (sum / totalIncome) * 100;
                        PopulateTableData("Gcash", 0, sum);
                        break;
                    case (int)TransactionTypes.LoadIncome:
                        IncomeChartData[4] = (sum / totalIncome) * 100;
                        PopulateTableData("Load", 0, sum);
                        break;
                    case (int)TransactionTypes.VanIncome:
                        IncomeChartData[5] = (sum / totalIncome) * 100;
                        PopulateTableData("Van", 0, sum);
                        break;
                    case (int)TransactionTypes.MotorIncome:
                        IncomeChartData[6] = (sum / totalIncome) * 100;
                        PopulateTableData("Motor", 0, sum);
                        break;
                    case (int)TransactionTypes.OtherIncome:
                        IncomeChartData[7] = (sum / totalIncome) * 100;
                        PopulateTableData("Other", 0, sum);
                        break;
                    default:
                        break;
                }
            }
        }
        private static void AddDailyLabels(DateRange range, List<string> labels)
        {
            DateTime startDate = (DateTime)range.Start;
            while (startDate <= range.End)
            {
                labels.Add(startDate.ToString("MMM d"));
                startDate = startDate.AddDays(1);
            }
        }
        private static void AddWeeklyLabels(DateRange range, List<string> labels)
        {
            DateTime startDate = (DateTime)range.Start;
            var currentDay = startDate.DayOfWeek;
            int daysTillCurrentDay = currentDay - DayOfWeek.Sunday;
            startDate = startDate.AddDays(-daysTillCurrentDay);
            while (startDate <= range.End)
            {
                int weekNum = (startDate.Day / 7) + 1;
                labels.Add(startDate.ToString($"MMM Week {weekNum}"));
                startDate = startDate.AddDays(7);
            }
        }
        private static void AddMonthlyLabels(DateRange range, List<string> labels)
        {
            DateTime startDate = (DateTime)range.Start;
            while (startDate <= range.End)
            {
                labels.Add(startDate.ToString("MMMM yyyy"));
                startDate = startDate.AddMonths(1);
            }
        }
        private static void AddQuarterlyLabels(DateRange range, List<string> labels)
        {
            DateTime startDate = (DateTime)range.Start;
            while (startDate <= range.End)
            {
                labels.Add(startDate.ToString("yyyy Q"));
                startDate = startDate.AddMonths(3);
            }
        }
        private static void AddYearlyLabels(DateRange range, List<string> labels)
        {
            DateTime startDate = (DateTime)range.Start;
            while (startDate <= range.End)
            {
                labels.Add(startDate.ToString("yyyy"));
                startDate = startDate.AddYears(1);
            }
        }

        private static bool IsIncome(int type)
        {
            if (type >= 1 && type <= 7) return true;
            if (type == 15) return true;
            return false;
        }
        private static bool IsExpense(int type)
        {
            if (type >= 8 && type <= 14) return true;
            if (type == 16) return true;
            return false;
        }

        public static double CalculateProfitMargin(double totalRevenue, double totalExpense)
        {
            var profit = totalRevenue - totalExpense;
            var profitMargin = (profit / totalRevenue) * 100;
            return profitMargin;
        }

        private void PopulateTableData(string key, int index, double value)
        {
            if (SummaryTableData.ContainsKey(key))
            {
                SummaryTableData[key][index] += value;
            }
            else
            {
                double[] val = { 0, 0 };
                val[index] += value;
                SummaryTableData.Add(key, val);
            }
        }

        private void TransformTableData()
        {
            TableData = new List<ReportsTable>();
            foreach (var row in SummaryTableData)
            {
                var profit = (decimal)(row.Value[0] - Math.Abs(row.Value[1]));
                var outMoney = (decimal)Math.Abs(row.Value[1]);

                var reportRow = new ReportsTable()
                {
                    Category = row.Key,
                    ProfitMargin = CalculateProfitMargin(row.Value[0],Math.Abs(row.Value[1])),
                    In = (decimal)row.Value[0],
                    Out = outMoney,
                    Profit = profit
                };
                TableData.Add(reportRow);
            }
            TableData = TableData.OrderByDescending(x => x.Profit).ToList();
        }
    }
    
    
}