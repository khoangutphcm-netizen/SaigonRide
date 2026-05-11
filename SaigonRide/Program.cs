using Microsoft.EntityFrameworkCore;
using SaigonRide.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System;

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string từ appsettings hoặc environment variables
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Trim()
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// Nếu không có connection string, throw exception
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration or environment variables.");
}

var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddScoped<SaigonRide.Services.RentalService>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "TempKeys")))
    .SetApplicationName("SaigonRide");

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

var viCulture = new System.Globalization.CultureInfo("vi");
viCulture.NumberFormat.NumberDecimalSeparator = ".";
viCulture.NumberFormat.CurrencyDecimalSeparator = ".";

var enCulture = new System.Globalization.CultureInfo("en-US");

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture: viCulture, uiCulture: viCulture),
    SupportedCultures = new[] { viCulture, enCulture },
    SupportedUICultures = new[] { viCulture, enCulture }
};

app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SaigonRide.Middlewares.BanCheckMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Intro}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();