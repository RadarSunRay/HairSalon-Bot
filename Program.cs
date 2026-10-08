using Bot.Data;
using Bot.EndPoints;
using Bot.Models;
using Bot.Services;
using Bot.State;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var connect = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(connect));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options => options.LoginPath = "/login");
builder.Services.AddAuthorization();

var botToken = builder.Configuration.GetSection("BotConfiguration")
.GetValue<string>("BotToken");

if (string.IsNullOrEmpty(botToken))
{
    throw new Exception("Telegram Bot Token is not configured in appsettings.json");
}

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddHostedService<TelegramBotBackgroundService>();

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<Context>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();

    await seeder.SeedAsync();
}
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapFallbackToFile("index.html").RequireAuthorization();

app.MapLogin();

app.MapGetUserAsync();

app.MapDeleteUserAsync();

app.MapLogout();

app.Run();
