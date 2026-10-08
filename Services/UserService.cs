using Bot.Data;
using Bot.Models;
using Bot.State;
using Microsoft.EntityFrameworkCore;

namespace Bot.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationContext _db;
        public UserService(ApplicationContext db)
        {
            _db = db;
        }
        public async Task<User> CreateUserAsync(SceneContext scene, string? userName)
        {
            var user = await _db.users.FirstOrDefaultAsync(x => x.Id == scene.chatId);

            if (user == null)
            {
                user = new User
                {
                    Id = scene.chatId,
                    TelegramUserName = userName ?? "No name",
                    PhoneNumber = "",
                };
                _db.users.Add(user);
                await _db.SaveChangesAsync();
            }
            return user;
        }

        public async Task SetPhoneNumberAsync(SceneContext scene, string phoneNumber)
        {
            if (!phoneNumber.StartsWith("+"))
            {
                phoneNumber = "+" + phoneNumber;
            }

            var user = await _db.users.FirstOrDefaultAsync(x => x.Id == scene.chatId);

            if (user != null)
            {
                user.PhoneNumber = phoneNumber;
                await _db.SaveChangesAsync();
            }

        }

        public async Task<User?> GetUser(long chatId)
        {
            return await _db.users.Include(x => x.SelectedBarber).FirstOrDefaultAsync(c => c.Id == chatId);
        }
    }
}
