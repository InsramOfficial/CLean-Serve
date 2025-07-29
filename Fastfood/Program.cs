using Fastfood.Data;
using Fastfood.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var con = builder.Configuration.GetConnectionString("Default_Connection").ToString();
builder.Services.AddDbContext<DataDbContext>(options => options.UseSqlServer(con));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(2); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/ControlPanel/Login";
		//options.AccessDeniedPath = "/ControlPanel/AccessDenied";
	});
//builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();        
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserIdMiddleware>(); 

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=HomeIndex}/{id?}");

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllerRoute(
//        name: "customRoute",
//        pattern: "mycustomroute/{controller=Home}/{action=HomeIndex}/{id?}"
//    );
//});

app.Run();
