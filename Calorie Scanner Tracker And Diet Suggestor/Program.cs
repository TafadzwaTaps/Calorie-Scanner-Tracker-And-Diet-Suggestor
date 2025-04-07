using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Microsoft.EntityFrameworkCore;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CalorieTrackerContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CalorieTrackerConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null) // 🔥 Add retry policy
    )
);

// ✅ Use Cookie Authentication (for session-based login)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    })
    .AddGoogle(googleOptions =>
    {
        var googleAuth = builder.Configuration.GetSection("Authentication:Google");
        googleOptions.ClientId = googleAuth["ClientId"];
        googleOptions.ClientSecret = googleAuth["ClientSecret"];
        googleOptions.CallbackPath = "/auth/google-response";  // This should match the redirect URI
    });
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
