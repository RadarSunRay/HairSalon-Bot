using Bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Bot.EndPoints
{
    public static class UserEndPoints
    {
        public static void MapGetUserAsync(this IEndpointRouteBuilder app)
        {
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
        }

        public static void MapDeleteUserAsync(this IEndpointRouteBuilder app)
        {
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
        }
    }
}
