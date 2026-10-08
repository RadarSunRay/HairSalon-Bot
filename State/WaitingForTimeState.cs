using Bot.Data;
using Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.State
{
    public class WaitingForTimeState : IBotState
    {
        private Dictionary<string, string> times = new()
        {
            ["get_nine"] = "9:00",
            ["get_ten"] = "10:00",
            ["get_eleven"] = "11:00",
            ["get_twelve"] = "12:00",
            ["get_thirteen"] = "13:00",
            ["get_fourteen"] = "14:00",
            ["get_fifteen"] = "15:00",
            ["get_sixteen"] = "16:00",
            ["get_seventeen"] = "17:00"
        };
        private InlineKeyboardMarkup GetTimeKeyboard()
        {
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var (callback, title) in times)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(title, callback)
                });
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "get_back")
            });
            return new InlineKeyboardMarkup(buttons);
        }

        public async Task EnterHandleAsync(SceneContext sceneContext)
        {
            await sceneContext.botClient.SendMessage(
                chatId: sceneContext.chatId,
                text: "Выберите время:",
                replyMarkup: GetTimeKeyboard(),
                cancellationToken: sceneContext.CancellationToken);
        }

        public async Task HandleInputAsync(SceneContext sceneContext, Update update)
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                var callback = update.CallbackQuery;
                string? data = callback.Data;

                using (var scope = sceneContext.serviceProvider.CreateScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    var user = await userService.GetUser(sceneContext.chatId);
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                    await sceneContext.botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: sceneContext.CancellationToken);
                    if (data == "get_back")
                    {
                        await sceneContext.ChangeState(sceneContext.WaitingForDayState);
                        return;
                    }

                    if (data != null && times.TryGetValue(data, out var selectedTime))
                    {
                        user.SelectedTime = selectedTime;
                        await db.SaveChangesAsync(sceneContext.CancellationToken);
                        await sceneContext.ChangeState(sceneContext.UserProfileState);
                    }
                }
            }
        }
    }
}
