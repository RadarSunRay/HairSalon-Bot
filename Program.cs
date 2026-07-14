using Microsoft.EntityFrameworkCore;
using Bot.Data;
using Telegram.Bot;
using Bot.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Bot.Models;

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

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapFallbackToFile("index.html").RequireAuthorization();
app.MapGet("/login", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    // html-форма для ввода логина/пароля
    string loginForm = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8' />
        <title>Login</title>
    </head>
    <body>
        <h2>Login Form</h2>
        <form method='post'>
            <p>
                <label>Login</label><br />
                <input name='email' />
            </p>
            <p>
                <label>Password</label><br />
                <input type='password' name='password' />
            </p>
            <input type='submit' value='Login' />
        </form>
    </body>
    </html>";
await context.Response.WriteAsync(loginForm);
});

app.MapGet("/api/users", async (ApplicationContext db) =>
{
    return await db.users
    .Include(u => u.SelectedBarber)
    .ToListAsync();
}).RequireAuthorization();

app.MapDelete("/api/bookings/{userId}", async (long userId, ApplicationContext db, ITelegramBotClient botClient) =>
{
    var user = await db.users.FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null) return Results.NotFound(new {message = "Пользователь не найден"});

    user?.SelectedService = "-";
    user?.SelectedTime = "-";
    user?.SelectedBarberId = null;
    user?.SelectedDay = null;
    await db.SaveChangesAsync();

    return Results.Ok(new {message = "Пользователь удален"});
}).RequireAuthorization();

app.MapPost("/login", async (HttpContext context, ApplicationContext db) =>
{
    var form = context.Request.Form;

    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
    {
        return Results.BadRequest(new {message = "Неправильный пароль/логин"});
    } 

    string? email = form["email"];
    string? password = form["password"];

    var admin = await db.admins.FirstOrDefaultAsync(u => u.name == email && u.password == password);

    if (admin != null)
    {
        var claims = new List<Claim> {new Claim(ClaimTypes.Name, email)};
        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        await context.SignInAsync(principal);
        return Results.Redirect("/");
    }
    else
    {
        return Results.BadRequest(new {message = "Неверный логин или пароль"});
    }

});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});
app.Run();
