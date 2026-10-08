using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.State
{
    public class MainMenuState : IBotState
    {
        private InlineKeyboardMarkup GetMenuKeyboard()
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Записаться на услугу", "get_service")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🗒 Моя запись", "get_profile")
            }
            });
            return keyboard;
        }
        
        public async Task EnterHandleAsync(SceneContext sceneContext)
        {
            await sceneContext.botClient.SendMessage(
                chatId: sceneContext.chatId,
                text: "Главное меню:",
                replyMarkup: GetMenuKeyboard(),
                cancellationToken: sceneContext.CancellationToken);
        }

        public async Task HandleInputAsync(SceneContext sceneContext, Update update)
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                var callback = update.CallbackQuery;
                string? data = callback.Data;
                switch (data)
                {
                    case "get_service":
                        {
                            await sceneContext.botClient.AnswerCallbackQuery(callback.Id, cancellationToken: sceneContext.CancellationToken);
                            await sceneContext.ChangeState(sceneContext.WaitingForServiceState);
                            break;
                        }
                    case "get_profile":
                        {
                            await sceneContext.botClient.AnswerCallbackQuery(callback.Id, cancellationToken: sceneContext.CancellationToken);
                            await sceneContext.ChangeState(sceneContext.UserProfileState);
                            break;
                        }
                }
            }
        }
    }
}
