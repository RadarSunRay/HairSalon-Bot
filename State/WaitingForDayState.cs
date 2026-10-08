using Bot.Data;
using Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.State
{
    public class WaitingForDayState : IBotState
    {
        private static string GetNextDateString(string dayName)
        {
            DayOfWeek targetDay = dayName switch
            {
                "Понедельник" => DayOfWeek.Monday,
                "Вторник" => DayOfWeek.Tuesday,
                "Среда" => DayOfWeek.Wednesday,
                "Четверг" => DayOfWeek.Thursday,
                "Пятница" => DayOfWeek.Friday,
                "Суббота" => DayOfWeek.Saturday,
                "Воскресенье" => DayOfWeek.Sunday,
                _ => DayOfWeek.Monday
            };

            DateTime today = DateTime.Today;
            int daysToAdd = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;

            return today.AddDays(daysToAdd).ToString("yyyy-MM-dd");
        }
        private Dictionary<string, string> days = new()
        {
            ["get_monday"] = "Понедельник",
            ["get_tuesday"] = "Вторник",
            ["get_wednesday"] = "Среда",
            ["get_thursday"] = "Четверг",
            ["get_friday"] = "Пятница",
            ["get_saturday"] = "Суббота",
            ["get_sunday"] = "Воскресенье"
        };
        private InlineKeyboardMarkup GetDayKeyboard()
        {
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var (callback, title) in days)
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
                text: "Выберите день недели:",
                replyMarkup: GetDayKeyboard(),
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
                        await sceneContext.ChangeState(sceneContext.WaitingForServiceState);
                        return;
                    }

                    if (data != null && days.TryGetValue(data, out var dayTitle))
                    {
                        user.SelectedDay = GetNextDateString(dayTitle);
                        await db.SaveChangesAsync(sceneContext.CancellationToken);
                        await sceneContext.ChangeState(sceneContext.WaitingForTimeState);
                    }
                }
            }
        }
    }
}
