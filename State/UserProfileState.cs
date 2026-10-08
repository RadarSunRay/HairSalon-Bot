using Bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.State
{
    public class UserProfileState : IBotState
    {
        private InlineKeyboardMarkup GetProfileKeyboard()
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Вернуться в главное меню", "get_back_menu")
            });
            return keyboard;
        }

        public async Task EnterHandleAsync(SceneContext sceneContext)
        {
            using (var scope = sceneContext.serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var user = await db.users.AsNoTracking().Include(x => x.SelectedBarber).FirstOrDefaultAsync(c => c.Id == sceneContext.chatId);

                if (user != null)
                {
                    string messageTextToSend = $"Ваша запись:\n\n" +
                           $"Услуга: <b>{user.SelectedService ?? "Записей нет"}</b>\n" +
                           $"Время: <b>{user.SelectedTime ?? "Время не назначено"}</b>\n" +
                           $"День: <b>{user.SelectedDay ?? "Дата не назначена"}</b>";

                    await sceneContext.botClient.SendMessage(
                    chatId: sceneContext.chatId,
                    text: messageTextToSend,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: GetProfileKeyboard(),
                    cancellationToken: sceneContext.CancellationToken
                    );
                }
            }
        }

        public async Task HandleInputAsync(SceneContext sceneContext, Update update)
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await sceneContext.botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: sceneContext.CancellationToken);
                if (update.CallbackQuery.Data == "get_back_menu")
                {
                    await sceneContext.ChangeState(sceneContext.MainMenuState);
                }
            }
        }
    }
}
