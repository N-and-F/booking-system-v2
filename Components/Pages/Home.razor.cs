using Microsoft.AspNetCore.Components;
using reservationSystem.Models;
using reservationSystem.Data;
using Microsoft.EntityFrameworkCore;


namespace reservationSystem.Components.Pages
{
    public partial class Home : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; }

        public List<User> Users { get; set; } = new List<User>();

        protected override void OnInitialized()
        {
            // Fetch data from DbContext
            NavigationManager.NavigateTo("/dashboard");
        }
    }
}
