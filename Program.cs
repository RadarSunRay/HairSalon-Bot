using Bot.Data;
using Bot.Models;
using Bot.Services;
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
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    await db.Database.MigrateAsync();

    if (!await db.admins.AnyAsync())
    {
        var admin = new Admin
        {
            name = "admin",
        };
        var password = builder.Configuration["Admin:Password"];

        if (string.IsNullOrEmpty(password))
        {
            throw new Exception("Admin password is not configured");
        }

        var hasher = new PasswordHasher<Admin>();
        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.admins.Add(admin);
        await db.SaveChangesAsync();
    }
}
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapFallbackToFile("index.html").RequireAuthorization();
app.MapGet("/login", () =>
{
    return Results.File("login.html", "text/html");
});

app.MapGet("/api/users", async (ApplicationContext db) =>
{
    var user = await db.users
    .Include(u => u.SelectedBarber)
    .ToListAsync();

    var userDto = user.Select(user => new UserDTO
    {
        SelectedService = user.SelectedService,
        TelegramUserName = user.TelegramUserName,
        PhoneNumber = user.PhoneNumber,
        SelectedBarber = user.SelectedBarber,
        SelectedDay = user.SelectedDay,
        SelectedTime = user.SelectedTime,
        Id = user.Id
    });
    return Results.Ok(userDto);
}).RequireAuthorization();

app.MapDelete("/api/bookings/{userId}", async (long userId, ApplicationContext db, ITelegramBotClient botClient) =>
{
    var user = await db.users.FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null) return Results.NotFound(new { message = "Пользователь не найден" });

    user?.SelectedService = "-";
    user?.SelectedTime = "-";
    user?.SelectedBarberId = null;
    user?.SelectedDay = null;
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Пользователь удален" });
}).RequireAuthorization();

app.MapPost("/login", async (HttpContext context, ApplicationContext db) =>
{
    var form = context.Request.Form;

    if (!form.ContainsKey("login") || !form.ContainsKey("password"))
    {
        return Results.BadRequest(new { message = "Неправильный пароль/логин" });
    }

    string? login = form["login"];
    string? password = form["password"];

    var admin = await db.admins.FirstOrDefaultAsync(u => u.name == login);

    if (admin == null) return Results.Redirect("/login?error=InvalidCredentials");

    var hasher = new PasswordHasher<Admin>();

    var result = hasher.VerifyHashedPassword(admin, admin.PasswordHash, password!);

    if (result != PasswordVerificationResult.Success)
    {
        return Results.Redirect("/login?error=InvalidCredentials");
    }

    var claims = new List<Claim> { new Claim(ClaimTypes.Name, login!) };
    var identity = new ClaimsIdentity(claims, "Cookies");
    var principal = new ClaimsPrincipal(identity);
    await context.SignInAsync(principal);
    return Results.Redirect("/");

});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});
app.Run();
