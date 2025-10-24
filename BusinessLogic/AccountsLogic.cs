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
using System.Security.Principal;
using Microsoft.Identity.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;
using reservationSystem.Components.Pages;

namespace reservationSystem.BusinessLogic
{
    public class AccountsLogic
    {
        private readonly DataSet _context;
        [Inject] public ILocalStorageService _localstorage { get; set; }
        public AccountsLogic(DataSet context, ILocalStorageService LocalStorage)
        {
            _context = context;
            _localstorage = LocalStorage;
        }

        public async Task<bool> IsUnique (string name, int id)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync (x => x.Name.ToLower() == name.ToLower());
            if (account == null) return true;
            if (account.Id == id) return true;
            return false;
        }
        public async Task<List<Account>> GetTableItems()
        {
            return await _context.Accounts.OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<IResult> CreateNewAccount(Account newAccount)
        {
            try
            {
                var newAmount = "₱" + FormatDecimal(newAccount.Amount);
                await _context.Accounts.AddAsync(newAccount);
                await _context.SaveChangesAsync();
                await MakeTransaction(newAccount.Id, newAccount.Amount, $"Account created with initial balance of {newAmount}.", (int)TransactionTypes.Transfer, null, true);
                return Results.Ok(newAccount);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> EditAccount(Account account)
        {
            try
            {
                var existingAccount = await _context.Accounts.Where(x => x.Id == account.Id).FirstOrDefaultAsync();
                if (existingAccount == null) return Results.NotFound(account.Id);
                var existingAccountCopy = await _context.Accounts.AsNoTracking().Where(x => x.Id == account.Id).FirstOrDefaultAsync();


                var prevAmount = "₱" + FormatDecimal(existingAccountCopy.Amount);
                var newAmount = "₱" + FormatDecimal(account.Amount);
                existingAccount.Name = account.Name;
                existingAccount.Amount = account.Amount;

                await _context.SaveChangesAsync();
                await MakeTransaction(existingAccount.Id, existingAccount.Amount, $"Amount changed to {newAmount} from {prevAmount}.", (int)TransactionTypes.Transfer, null, true);

                return Results.Ok(account);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> DeleteAccount(int id)
        {
            try
            {
                var existingAccount = await _context.Accounts.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (existingAccount == null) return Results.NotFound(id);

                _context.Remove(existingAccount);
                await _context.SaveChangesAsync();
                return Results.Ok(existingAccount);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<List<AccountTransaction>> GetAllTransactions(List<int>? types, string dateRange, List<int>? accountIds, string orderBy = "DESC")
        {
            var (startDate, endDate) = GetDateRange(dateRange);

            var baseQuery = _context.AccountTransactions;
            var filteredQuery = baseQuery
                .Where(x => types == null || types.Contains(x.Type))
                .Where(x => x.Date >= startDate && x.Date <= endDate)
                .Where(x => accountIds == null || accountIds.Count == 0 || accountIds.Any(id => id == x.AccountId));
            var orderedQuery = filteredQuery
                .OrderByDescending(x => x.Date);
            if (orderBy.ToLower() == "asc")
            {
                orderedQuery = orderedQuery.OrderBy(x => x.Date);
            }

            return await orderedQuery.ToListAsync();
        }

        public async Task<List<AccountTransaction>> GetAllTransactions(DateTime? startDate, DateTime? endDate, List<int>? types = null, List<int>? accountIds = null, string orderBy = "DESC")
        {
            var trueEndDate = endDate?.AddDays(1).AddSeconds(-1);
            var baseQuery = _context.AccountTransactions;
            var filteredQuery = baseQuery
                .Where(x => types == null || types.Contains(x.Type))
                .Where(x => x.Date >= startDate && x.Date <= trueEndDate)
                .Where(x => accountIds == null || accountIds.Count == 0 || accountIds.Any(id => id == x.AccountId));
            var orderedQuery = filteredQuery
                .OrderByDescending(x => x.Date);
            if (orderBy.ToLower() == "asc")
            {
                orderedQuery = orderedQuery.OrderBy(x => x.Date);
            }

            return await orderedQuery.ToListAsync();
        }


        public async Task<List<AccountTransaction>> GetTransactions(int accountId, string orderBy = "DESC")
        {
            var transactions = await _context.AccountTransactions
                                    .Where(x => x.AccountId == accountId)
                                    .Where(x => x.Date <= DateTime.Now.AddDays(1).AddSeconds(-1) && x.Date >= DateTime.Today.AddDays(-30))
                                    .OrderByDescending(x => x.Date)
                                    .ToListAsync();

            if (orderBy != "DESC")
            {
                transactions = [.. transactions.OrderBy(x => x.Date)];
            }

            return transactions;
        }

        public async Task<IResult> MakeTransaction(int accountId, decimal amount, string description, int type, DateTime? date, bool isFromAccountPage = false)
        {
            try
            {
                var existingAccount = await _context.Accounts.Where(x => x.Id == accountId).FirstOrDefaultAsync();
                if (existingAccount == null) return Results.NotFound(accountId);

                //Transaction is not from accounts page -> edit account
                if (!isFromAccountPage)
                {
                    existingAccount.Amount += amount;
                }
                var currentDate = DateTime.Now;
                var transaction = new AccountTransaction()
                {
                    AccountId = accountId,
                    Description = description,
                    Date = date ?? DateTime.Now,
                    Type = type,
                    Amount = amount,
                    CreatedAt = currentDate,
                };
                await _context.AccountTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                return Results.Ok(existingAccount);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> MakeTransaction(AccountTransaction accountTransaction, bool isFromAccountPage = false)
        {
            try
            {
                var existingAccount = await _context.Accounts.Where(x => x.Id == accountTransaction.AccountId).FirstOrDefaultAsync();
                if (existingAccount == null) return Results.NotFound(accountTransaction.AccountId);

                if (!isFromAccountPage)
                {
                    existingAccount.Amount += accountTransaction.Amount;
                }

                AccountTransaction transaction = new AccountTransaction()
                {
                    AccountId = accountTransaction.AccountId,
                    Description = accountTransaction.Description,
                    Date = accountTransaction.Date,
                    Type = accountTransaction.Type,
                    Amount = accountTransaction.Amount,
                    CreatedAt = DateTime.Now,
                };
                await _context.AccountTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                return Results.Ok(existingAccount);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> DeleteTransaction(AccountTransaction transaction)
        {
            try
            {
                var existingTransaction = await _context.AccountTransactions.Where(x => x.Id == transaction.Id).FirstOrDefaultAsync();
                if (existingTransaction == null) return Results.NotFound(transaction.Id);

                await MakeTransaction(existingTransaction.AccountId, -existingTransaction.Amount, "Transaction deleted with description: '" + existingTransaction.Description + "'.", (int)TransactionTypes.Transfer, null);
                _context.AccountTransactions.Remove(existingTransaction);

                await _context.SaveChangesAsync();
                return Results.Ok(existingTransaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        public async Task<IResult> TransferBalance(int senderId, int receiverId, decimal amount, decimal fee, DateTime? date, bool isLoadType = false)
        {
            try
            {
                var accounts = await GetTableItems();
                var sender = accounts.First(x => x.Id == senderId);
                var receiver = accounts.First(x => x.Id == receiverId);
                sender.Amount -= amount + fee;

                var description = $"{receiver.Name} received ₱{FormatDecimal(amount)} from {sender.Name}.";
                if (!isLoadType)
                {
                    receiver.Amount += amount;
                    await MakeTransaction(receiver.Id, amount, description, (int)TransactionTypes.Transfer, date, true);
                }
                description = $"{sender.Name} transferred ₱{FormatDecimal(amount)} to {receiver.Name}.";
                await MakeTransaction(sender.Id, -amount, description, (int)TransactionTypes.Transfer, date, true);
                if (fee > 0 && !isLoadType)
                {
                    description = $"{sender.Name} was charged ₱{FormatDecimal(fee)} for the transfer fee.";
                    await MakeTransaction(sender.Id, -fee, description, (int)TransactionTypes.TransferFee, date, true);
                }
                await _context.SaveChangesAsync();


                return Results.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
            
        }

        public static string FormatDecimal(decimal value)
        {
            return value.ToString("N2", new System.Globalization.CultureInfo("en-US"));
        }
        
        public static string FormatDecimal(double value)
        {
            return value.ToString("N2", new System.Globalization.CultureInfo("en-US"));
        }

        public static string FormatMoney(decimal value)
        {
            var moneyString = "₱" + value.ToString("N2", new System.Globalization.CultureInfo("en-US"));
            if (value < 0)
            {
                value = Math.Abs(value);
                moneyString = "-₱" + value.ToString("N2", new System.Globalization.CultureInfo("en-US"));

            }
            return moneyString;
        }

        public static string MapType(int type)
        {
            return type switch
            {
                (int)TransactionTypes.Transfer => "Transfer",
                (int)TransactionTypes.HotelIncome => "Hotel Income",
                (int)TransactionTypes.StoreIncome => "Store Income",
                (int)TransactionTypes.GcashCashInIncome => "Gcash Cash In",
                (int)TransactionTypes.GcashCashOutIncome => "Gcash Cash Out",
                (int)TransactionTypes.LoadIncome => "Load Income",
                (int)TransactionTypes.VanIncome => "Van Income",
                (int)TransactionTypes.MotorIncome => "Motor Income",
                (int)TransactionTypes.HotelExpense => "Hotel Expense",
                (int)TransactionTypes.StoreExpense => "Store Expense",
                (int)TransactionTypes.GcashExpense => "Gcash Expense",
                (int)TransactionTypes.LoadExpense => "Load Expense",
                (int)TransactionTypes.VanExpense => "Van Expense",
                (int)TransactionTypes.MotorExpense => "Motor Expense",
                (int)TransactionTypes.TransferFee => "Transfer Fee",
                (int)TransactionTypes.OtherIncome => "Other Income",
                (int)TransactionTypes.OtherExpense => "Other Expense",
                _ => "Not specified"
            };
        }

        public static bool IsEditableTransaction(int type)
        {
            if (type == (int)TransactionTypes.HotelIncome || 
                type == (int)TransactionTypes.Transfer || 
                type == (int)TransactionTypes.OtherIncome || 
                type == (int)TransactionTypes.OtherExpense) return false;
            return true;
        }

        public static (DateTime?, DateTime?) GetDateRange(string option)
        {
            var today = DateTime.Today;

            switch (option)
            {
                case "Previous Month":
                    int currentYear = today.Year;
                    int currentMonth = today.Month;
                    DateTime currentMonthDateTime = new DateTime(currentYear, currentMonth, 1);
                    DateTime prevMonth = currentMonthDateTime.AddMonths(-1);
                    DateTime endDate = currentMonthDateTime.AddDays(-1);
                    return (prevMonth, endDate);
                case "Last 7 Days":
                    return (today.AddDays(-6), today.AddDays(1));
                case "Last 30 Days":
                    return (today.AddDays(-29), today.AddDays(1));

                case "Today":
                    return (today, today.AddDays(1));

                case "This Week":
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                    var endOfWeek = today.AddDays(7-(int)today.DayOfWeek);
                    return (startOfWeek, endOfWeek);

                case "This Month":
                    currentYear = today.Year;
                    currentMonth = today.Month;
                    DateTime startDate = new DateTime(currentYear, currentMonth, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    return (startDate,endDate);

                case "This Year":
                    currentYear = today.Year;
                    startDate = new DateTime(currentYear, 1, 1);
                    endDate = startDate.AddYears(1);
                    return (startDate, endDate);

                default:
                    throw new ArgumentException("Invalid date range option");
            }
        }

    }
}
