using Bot.Data;
using Bot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.State
{
    public class WaitingForServiceState : IBotState
    {
        private Dictionary<string, string> services = new()
        {
            ["get_man_hair"] = "✂️ Мужская стрижка",
            ["get_woman_hair"] = "💇‍♀️ Женская стрижка",
            ["get_colors"] = "🎨 Окрашивание",
            ["get_care_hair"] = "💆‍♂️ Уход за волосами"

        };
        private InlineKeyboardMarkup GetServiceKeyboard()
        {
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var (callback, title) in services)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(title, callback)
                });
            }
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Вернуться в главное меню", "get_back_menu")
            });
            return new InlineKeyboardMarkup(buttons);
        }

        public async Task EnterHandleAsync(SceneContext sceneContext)
        {
            await sceneContext.botClient.SendMessage(
                chatId: sceneContext.chatId,
                text: "Выберите услугу:",
                replyMarkup: GetServiceKeyboard(),
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
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                    var user = await userService.GetUser(sceneContext.chatId);
                    await sceneContext.botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: sceneContext.CancellationToken);
                    if (data == "get_back_menu")
                    {
                        await sceneContext.ChangeState(sceneContext.MainMenuState);
                        return;
                    }

                    if (data != null && services.TryGetValue(data, out var serviceTitle))
                    {
                        var barbers = await db.barbers.AsNoTracking().FirstOrDefaultAsync(x => x.special == serviceTitle);
                        user.SelectedService = serviceTitle;
                        user.SelectedBarber = barbers;
                        await db.SaveChangesAsync(sceneContext.CancellationToken);
                        await sceneContext.ChangeState(sceneContext.WaitingForDayState);
                    }
                }
            }
        }
    }
}
