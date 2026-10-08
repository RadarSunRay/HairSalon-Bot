using Bot.Data;
using Bot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bot.EndPoints
{
    public static class LogEndPoints
    {
        public static void MapLogin(this IEndpointRouteBuilder app)
        {
            app.MapGet("/login", () =>
            {
                return Results.File("login.html", "text/html");
            });

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
        }

        public static void MapLogout(this IEndpointRouteBuilder app)
        {
            app.MapGet("/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            });
        }
    }
}
