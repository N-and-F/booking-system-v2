using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using reservationSystem.Components;
using reservationSystem.Data;
using MudBlazor.Services;
using reservationSystem.BusinessLogic;
using MudBlazor;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.WithOrigins("http://localhost:5173")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.Services.AddScoped<UsersLogic>();
builder.Services.AddScoped<RoomsLogic>();
builder.Services.AddScoped<BookingsLogic>();
builder.Services.AddScoped<GuestsLogic>();
builder.Services.AddScoped<HotelsLogic>();
builder.Services.AddScoped<ReportsLogic>();
builder.Services.AddScoped<AccountsLogic>();


builder.Services.AddDbContext<DataSet>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ServerConnection")));

builder.Services.AddMudServices();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;

    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddBlazoredLocalStorage();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseCors("CorsPolicy");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
