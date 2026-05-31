using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Services;

// i need to study and learn about the pipelines and middleware in asp net core****

var builder = WebApplication.CreateBuilder(args);

// Add services to the container FIRST
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection configuration.");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
    });
// Only call this ONCE
builder.Services.AddControllersWithViews();

builder.Services.Configure<PlateRecognizerOptions>(
    builder.Configuration.GetSection("PlateRecognizer"));

builder.Services.AddHttpClient<IPlateRecognizerClient, PlateRecognizerClient>();

// QR code generation
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
// EDFali/DPay payment helper
builder.Services.AddTransient<IEdfaliService, EdfaliService>();





var app = builder.Build();






// Middleware ORDER IS CRITICAL
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // This enables wwwroot files (CSS/JS)
app.UseRouting();

// Authentication BEFORE Authorization
app.UseAuthentication(); 
app.UseAuthorization();

// Map routes enasuring default route and accespting id parameter
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();