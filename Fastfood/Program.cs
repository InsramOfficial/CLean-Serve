using Fastfood.Data;
using Fastfood.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var con = builder.Configuration.GetConnectionString("Default_Connection")!;
builder.Services.AddDbContext<DataDbContext>(options => options.UseSqlServer(con));
builder.Services.AddDbContext<DataDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<LowStockNotificationFilter>(); // ✅ Register globally
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/ControlPanel/Login";
        //options.AccessDeniedPath = "/ControlPanel/AccessDenied";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();  // ✅ Keep only once
app.UseMiddleware<UserIdMiddleware>();

// ✅ Define multiple routes properly
app.MapControllerRoute(
    name: "home",
    pattern: "{controller=Home}/{action=HomeIndex}/{id?}");

app.MapControllerRoute(
    name: "controlpanel",
    pattern: "{controller=ControlPanel}/{action=Index}/{id?}");

app.Run();
