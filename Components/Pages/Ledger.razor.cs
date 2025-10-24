using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.JSInterop;
using MudBlazor;
using Newtonsoft.Json;
using reservationSystem.BusinessLogic;
using reservationSystem.Components.Dialogs;
using reservationSystem.Enums;
using reservationSystem.Models;
using System.ComponentModel;

namespace reservationSystem.Components.Pages
{
    public partial class Ledger
    {

        public List<AccountTransaction> AccountTransactions { get; set; } = new List<AccountTransaction>();
        public List<AccountTransaction> FilteredTransactionList = new List<AccountTransaction>();
        public List<Account> Accounts { get; set; } = new List<Account>();

        [Inject] private AccountsLogic AccountsLogic { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ILocalStorageService _localstorage { get; set; }
        [Inject] private NavigationManager? NavigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        public bool IsLoading { get; set; } = false;
        public int Role { get; set; } = 2;
        public string HotelColor { get; set; }
        public string DateRange { get; set; } = "Last 7 Days";
        public IEnumerable<string> Types { get; set; }
        public IEnumerable<int> AccountFiltered { get; set; }

        public int PageSize { get; set; } = 50;
        public event PropertyChangedEventHandler PropertyChanged;

        public string SearchFilter { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            await Initialize();
        }

        private async Task Initialize(List<int>? types = null)
        {
            IsLoading = true;
            var selectedAccounts = AccountFiltered?.ToList();
            AccountTransactions = await AccountsLogic.GetAllTransactions(types, DateRange, selectedAccounts);
            FilteredTransactionList = new List<AccountTransaction>(AccountTransactions);
            Accounts = await AccountsLogic.GetTableItems();
            Role = int.Parse(await _localstorage.GetItemAsStringAsync("Role"));
            HotelColor = (await _localstorage.GetItemAsStringAsync("HotelColor")).Replace('\"', ' ').Trim();
            if (Role > (int)Enums.RoleTypes.Manager)
            {
                NavigationManager?.NavigateTo("/", forceLoad: true);
            }
            IsLoading = false;
        }

        public string FormatDate(DateTime? date) 
        {
            return date?.ToString("MMMM dd, yyyy hh:mm tt") ?? "";
        }


        /* public void OnSearchFilterChanged(string filter)
         {
             SearchFilter = filter;
             FilteredUserList = string.IsNullOrEmpty(SearchFilter)
                 ? UserList
                 : UsersLogic.GetTableItemsFiltered(UserList, SearchFilter);

         }*/

        private async Task HandleSelectedItemChanged(AccountTransaction accountTransaction, string type, string transactionType)
        {
            if(Accounts.Count == 0)
            {
                Snackbar.Add("Please add accounts in the Accounts Page.", Severity.Error);
                return;
            }
            var parameters = new DialogParameters<LedgerRecordDialog>
            {
                { x => x.AccountTransaction, accountTransaction },
                { x => x.DialogType, type },
                { x => x.TransactionType, transactionType },
                { x => x.Accounts, Accounts}
            };

            var dialog = await DialogService.ShowAsync<LedgerRecordDialog>(null, parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await Initialize();
            }
        }

        private async Task HandleSelectedFilterChanged()
        {
            var parameters = new DialogParameters<FilterDialog>
            {
                { x => x.FilterType, "Ledger" },
                { x => x.SelectedDate, DateRange },
                { x => x.SelectedTransactionTypes, Types },
                { x => x.SelectedAccounts, AccountFiltered },
                { x => x.AccountList, Accounts }

            };

            var dialog = await DialogService.ShowAsync<FilterDialog>(null, parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                try
                {
                    var resultObject = JsonConvert.DeserializeObject<dynamic>((string)result.Data);
                    List<int> types = new();
                    List<string> transactionTypes = new();
                    List<string> transactionTypesResultObject = resultObject.transactionTypes.ToObject<List<string>>();

                    if (transactionTypesResultObject.Contains("Income") || transactionTypesResultObject.Count == 0)
                    {
                        if (transactionTypesResultObject.Contains("Income")) transactionTypes.Add("Income");
                        types.AddRange(new[] {
                            (int)TransactionTypes.HotelIncome,
                            (int)TransactionTypes.VanIncome,
                            (int)TransactionTypes.StoreIncome,
                            (int)TransactionTypes.MotorIncome,
                            (int)TransactionTypes.LoadIncome,
                            (int)TransactionTypes.GcashCashInIncome,
                            (int)TransactionTypes.GcashCashOutIncome,
                        });
                    }

                    if (transactionTypesResultObject.Contains("Expenses") || transactionTypesResultObject.Count == 0)
                    {
                        if (transactionTypesResultObject.Contains("Expenses")) transactionTypes.Add("Expenses");
                        types.AddRange(new[] {
                            (int)TransactionTypes.HotelExpense,
                            (int)TransactionTypes.VanExpense,
                            (int)TransactionTypes.StoreExpense,
                            (int)TransactionTypes.MotorExpense,
                            (int)TransactionTypes.LoadExpense,
                            (int)TransactionTypes.GcashExpense,
                        });
                    }

                    if (transactionTypesResultObject.Contains("Transfers") || transactionTypesResultObject.Count == 0)
                    {
                        if (transactionTypesResultObject.Contains("Transfers")) transactionTypes.Add("Transfers");
                        types.AddRange(new[] {
                            (int)TransactionTypes.Transfer,
                            (int)TransactionTypes.TransferFee
                        });
                    }

                    if (transactionTypesResultObject.Contains("Others") || transactionTypesResultObject.Count == 0)
                    {
                        if (transactionTypesResultObject.Contains("Others")) transactionTypes.Add("Others");
                        types.AddRange(new[] {
                            (int)TransactionTypes.OtherExpense,
                            (int)TransactionTypes.OtherIncome
                        });
                    }
                    AccountFiltered = resultObject.accounts.ToObject<List<int>>();
                    DateRange = (string)resultObject.date;
                    Types = [.. transactionTypes];
                    await Initialize(types);
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                    // Log the exception or handle it appropriately
                }
            }

        }
    }
}