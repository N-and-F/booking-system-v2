using Azure;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using reservationSystem.BusinessLogic;
using reservationSystem.Data;
using reservationSystem.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace reservationSystem.Components.Pages
{
    public partial class Login : ComponentBase
    { 
        public string UsernameValue { get; set; } = "";
        public string PasswordValue { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public bool LoggingIn { get; set; } = false;

        public Hotel Hotel { get; set; }
        [Parameter] public EventCallback<bool> OnLoginSuccess { get; set; }
        [Inject] public DataSet _context { get; set; }
		[Inject] public HotelsLogic HotelsLogic { get; set; }


		public async Task HandleLogin()
		{
			LoggingIn = true;
			int maxRetries = 3; // Define a maximum number of retries
			int retryCount = 0;

			while (retryCount < maxRetries)
			{
				try
				{
					var user = await _context.Users
						.Where(x => x.Username.ToLower() == UsernameValue.ToLower() && x.Password == PasswordValue)
						.FirstOrDefaultAsync();

					if (user == null)
					{
						ErrorMessage = (string.IsNullOrEmpty(UsernameValue) || string.IsNullOrEmpty(PasswordValue))
							? "Please fill in required fields" : "Invalid Credentials. Please try again";

						await OnLoginSuccess.InvokeAsync(false);
					}
					else
					{
						var hotel = await HotelsLogic.GetHotel(user.HotelId);
						await _localstorage.SetItemAsync("Username", user.Username);
						await _localstorage.SetItemAsync("Role", user.Role);
						await _localstorage.SetItemAsync("Id", user.Id);
                        await _localstorage.SetItemAsync("HotelId", user.HotelId);
						await _localstorage.SetItemAsync("HotelColor", hotel.PrimaryColor);
                        await _localstorage.SetItemAsync("HotelLogo", hotel.LogoUrl);
                        await OnLoginSuccess.InvokeAsync(true);
					}
					break; // Exit the loop if login is successful
				}
				catch (SqlException ex)
				{
					// Log the error message to the browser console
					await JSRuntime.InvokeVoidAsync("console.error", ex);
					// Increment the retry count and wait before retrying
					retryCount++;
					await Task.Delay(3000); // Wait for 3 seconds before retrying
				}
				catch (Exception ex)
				{
					// Handle other exceptions by breaking the loop
					ErrorMessage = ex.Message;
					break;
				}
			}

			LoggingIn = false;
		}


	}
}