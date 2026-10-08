using Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.State
{
    public class SetPhoneNumberState : IBotState
    {
        private ReplyKeyboardMarkup GetPhoneKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
            KeyboardButton.WithRequestContact("📱 Поделиться номером телефона")
        })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
        }

        public async Task EnterHandleAsync(SceneContext sceneContext)
        {
            await sceneContext.botClient.SendMessage(
                chatId: sceneContext.chatId,
                text: "Чтобы записаться на услуги, зарегистрируйте свой номер телефона",
                replyMarkup: GetPhoneKeyboard(),
                cancellationToken: sceneContext.CancellationToken);
        }

        public async Task HandleInputAsync(SceneContext scene, Update update)
        {
            if (update.Message?.Contact is { } contact)
            {
                using (var scope = scene.serviceProvider.CreateScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    await userService.SetPhoneNumberAsync(scene, contact.PhoneNumber);

                    await scene.botClient.SendMessage(
                        chatId: scene.chatId,
                        text: $"✅ <b>Регистрация успешно завершена!</b>\n\nЧтобы записаться на услуги нажмите кнопку ниже",
                        replyMarkup: new ReplyKeyboardRemove(),
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: scene.CancellationToken
                    );
                    await scene.ChangeState(scene.MainMenuState);
                }
            }
        }
    }
}
