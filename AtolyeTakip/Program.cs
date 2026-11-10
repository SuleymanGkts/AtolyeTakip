using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using AtolyeTakip.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Yolunu Projenin Ana Kök Dizinine Sabitleme
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "atolye.db");

// 2. Servis Kayýtlarý
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AtolyeDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Hesap/Giris";
        options.AccessDeniedPath = "/Hesap/Giris";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });

// 3. Uygulamanýn Derlenmesi
var app = builder.Build();

// 4. Veritabanýný Baþlatma (EnsureDeleted KALDIRILDI)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AtolyeDbContext>();

    // DÝKKAT: EnsureDeleted() KALDIRILDI! 
    // Sadece dosya yoksa sýfýrdan oluþturur, dosya varsa mevcut verilere ASLA dokunmaz:
    db.Database.EnsureCreated();
}

// 5. HTTP Ýstek Hattý
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Hesap}/{action=Giris}/{id?}");

app.Run();