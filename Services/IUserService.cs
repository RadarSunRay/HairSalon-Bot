using Bot.Models;
using Bot.State;

namespace Bot.Services
{
    public interface IUserService
    {
        public Task<User> CreateUserAsync(SceneContext scene, string userName);
        public Task SetPhoneNumberAsync(SceneContext scene, string phoneNumber);
        public Task<User> GetUser(long chatId);
    }
}
