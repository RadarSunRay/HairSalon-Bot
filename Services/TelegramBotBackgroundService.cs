using Bot.Data;
using Bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Services;
public class TelegramBotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _botclient;
    private readonly ILogger<TelegramBotBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string[] _allowtime = new string[] {"9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00"};
    private readonly Dictionary<string, DayOfWeek> _daysOfWeek = 
    new Dictionary<string, DayOfWeek>
    {
        {"Понедельник", DayOfWeek.Monday},
        {"Вторник", DayOfWeek.Tuesday},
        {"Среда", DayOfWeek.Wednesday},
        {"Четверг", DayOfWeek.Thursday},
        {"Пятница", DayOfWeek.Friday},
        {"Суббота", DayOfWeek.Saturday},
        {"Воскресенье", DayOfWeek.Sunday}
    };
    public TelegramBotBackgroundService(ITelegramBotClient botClient,
    ILogger<TelegramBotBackgroundService> logger, IServiceProvider serviceProvider)
    {
        _botclient = botClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Bot Hosted Service запущен");

         var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botclient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("Telegram Bot начал слушать сообщения");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        var chatId = message.Chat.Id;
        if (message.Contact is { } contact)
        {
            string phoneNumber = contact.PhoneNumber;

            if(!phoneNumber.StartsWith("+"))
            {
                phoneNumber = "+" + phoneNumber;
            }
            _logger.LogInformation($"Получен контакт от чата {chatId} - {phoneNumber}");

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var user = await db.users.FindAsync(chatId);

                if (user != null)
                {
                    user.PhoneNumber = phoneNumber;
                    await db.SaveChangesAsync();
                    _logger.LogInformation($"Пользователю {user.TelegramUserName} привязан номер {phoneNumber}");
                }
            }
            await botClient.SendMessage(
                chatId: chatId,
                text: $"✅ <b>Регистрация успешно завершена!</b>\n\nНомер {phoneNumber}, чтобы записаться на услуги нажмите кнопку ниже",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: GetMenuKeyboard(),
                cancellationToken: cancellationToken
            );
            return;
        }
        if (message.Text is not { } messageText) return;

        if (messageText == "/start")
        {
            string userName = message.From?.Username ?? "Undefined";

            bool isNewUser = false;

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var user = await db.users.FindAsync(chatId);

                if (user == null)
                {
                    user = new Models.User
                    {
                        Id = chatId,
                        TelegramUserName = userName,
                        PhoneNumber = ""
                    };
                    db.users.Add(user);
                    await db.SaveChangesAsync();
                    isNewUser = true;
                    _logger.LogInformation($"Новый пользователь {userName} записан в БД");
                }
                else if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    isNewUser = true;
                }
                if (isNewUser)
                {
                    await botClient.SendMessage(

                        chatId: chatId,
                        text: $"Привет, {message.From?.FirstName}!\n\nЧтобы записаться на стрижку необходим ваш номер телефона\n\nПодтвердите свой номер, нажав на кнопку ниже",
                        replyMarkup: GetContactKeyboard(),
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendMessage(
                chatId: chatId,
                text: $"С возвращением <b>{userName}!</b>\n\nЧтобы записаться на услугу нажмите на кнопку меню ниже",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: GetMenuKeyboard(),
                cancellationToken: cancellationToken
                    );
                }
            }
            
            await UpdateUserState(chatId, "None");
        }
        else if (messageText == "📝 Записаться на услугу")
        {
            await UpdateUserState(chatId, "WaitingForService");

            await botClient.SendMessage(
                chatId: chatId,
                text: "<b>Выберите желаемую услугу из списка:</b>",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: GetServicesKeyboard(),
                cancellationToken: cancellationToken
            );

        }
        else if (messageText == "🔙 Вернуться в главное меню")
        {
            await UpdateUserState(chatId, "None");

            await botClient.SendMessage(
                chatId: chatId,
                text: "<b>Главное меню:</b>",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: GetMenuKeyboard(),
                cancellationToken: cancellationToken
            );
        }
        else if (messageText.Contains("🗒 Моя запись"))
        {
            string selectedService = string.Empty;
            string selectedTime = string.Empty;
            string selectedDay = string.Empty;
            string serviceDisplay = string.Empty;
            string messageTextToSend = string.Empty;
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                var user = await db.users.FindAsync(chatId);
                if (user != null)
                {
                    selectedService = user.SelectedService ?? "Записей нет";
                    selectedTime = user.SelectedTime ?? "Время не назначено";
                    selectedDay = user.SelectedDay?.ToString("dd.MM.yyyy") ?? "Дата не назначена";

                    messageTextToSend = $"Ваша запись:\n\n" +
                           $"Услуга: <b>{selectedService}</b>\n" +
                           $"Время: <b>{selectedTime}</b>\n" +
                           $"День: <b>{selectedDay}</b>";

                        if (serviceDisplay != "Записей нет")
                        {
                            messageTextToSend += "\n\nПожалуйста, не опаздывайте!";
                        }
                }
            }
            await botClient.SendMessage(
                chatId: chatId,
                text: messageTextToSend,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken:cancellationToken
            );
        }
        else
        {
            _logger.LogInformation($"Пользователь {chatId} прислал сообщение {messageText}");
            string userState = await GetUserState(chatId);
            if (userState == "WaitingForService")
            {
                string special = string.Empty;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                    var user = await db.users.FindAsync(chatId);
                    var barber = await db.barbers.FirstOrDefaultAsync(u => u.special == messageText);

                    if (user != null) 
                    {
                        if (await db.barbers.AnyAsync(u => u.special == messageText)) 
                        {
                            user.SelectedService = messageText;
                            user?.SelectedBarberId = barber.Id;
                            await db.SaveChangesAsync();
                            await botClient.SendMessage(
                            chatId: chatId,
                            text: $"Отличный выбор! Вы выбрали услугу: <b>{messageText}</b>.\n\nТеперь выберите день удобный для вас 📆",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            replyMarkup: GetDayKeyboard(),
                            cancellationToken: cancellationToken
                            );
                            await UpdateUserState(chatId, "WaitingForDay");
                        }
                        else
                        {
                            await botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, используйте кнопки меню 👇",
                            replyMarkup: GetServicesKeyboard(),
                            cancellationToken: cancellationToken
                            );
                        }
                    }
                }
                _logger.LogInformation($"DEBUG: Пользователь {chatId} в состоянии {userState}");
            }
            else if (userState == "WaitingForDay")
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    Books? books = new Books();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                    var user = await db.users.FindAsync(chatId);
                    if (user != null)
                    {
                        if (_daysOfWeek.ContainsKey(messageText))
                        {
                            var currentDays = DateTime.Today.DayOfWeek;

                            if (!_daysOfWeek.TryGetValue(messageText, out DayOfWeek selectedDay))
                            {
                                _logger.LogError($"Не удалось получить DayOfWeek для {messageText}");
                                return;
                            }

                            int daysToAdd = (int)selectedDay - (int)currentDays;

                            if (daysToAdd < 0)
                            {
                                daysToAdd += 7;
                            }

                            DateTime bookingDay = DateTime.Today.AddDays(daysToAdd);

                            user.SelectedDay = DateTime.SpecifyKind(bookingDay, DateTimeKind.Utc);

                            await db.SaveChangesAsync();
                            await UpdateUserState(chatId, "WaitingForTime");

                            await botClient.SendMessage(
                            chatId: chatId,
                            text: $"Вы выбрали <b>{messageText}</b>, {bookingDay:dd.MM.yyyy}.\n\nТеперь выберите удобное время:",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            replyMarkup: GetTimeKeyboard(),
                            cancellationToken: cancellationToken
                            );
                        }
                        else if (messageText.Contains("🔙 Назад"))
                        {
                            await UpdateUserState(chatId, "WaitingForService");

                            await botClient.SendMessage(
                            chatId: chatId,
                            text: "<b>Выберите желаемую услугу из списка:</b>",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            replyMarkup: GetServicesKeyboard(),
                            cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, используйте кнопки меню 👇",
                            replyMarkup: GetDayKeyboard(),
                            cancellationToken: cancellationToken
                            );
                        }
                    }
                }
            }
            else if (userState == "WaitingForTime")
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    Books? books = new Books(); 
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                    var user = await db.users.FindAsync(chatId);
                    if (user != null)
                    {
                        if (_allowtime.Contains(messageText))
                        {
                            if (TimeSpan.TryParse(messageText, out var time))
                        {
                            var localDatetime = DateTime.Today.Add(time);
                            books.BookTime = DateTime.SpecifyKind(localDatetime, DateTimeKind.Utc);
                        
                            user?.SelectedTime = messageText;
                            books.UserId = user.Id;
                            books.BarberId = user.SelectedBarberId ?? 1;
                        
                            db.books.Add(books);
                            await db.SaveChangesAsync();
                        }
                            await botClient.SendMessage(
                            chatId: chatId,
                            text: $"✅Вы записаны на <b>{messageText}</b>.\n\nПожалуйста не опаздывайте!",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            replyMarkup: GetMenuKeyboard(),
                            cancellationToken: cancellationToken
                            );
                            await UpdateUserState(chatId, "None");
                        }
                        else if (messageText == "🔙 Назад")
                        {
                            await UpdateUserState(chatId, "WaitingForDay");

                            await botClient.SendMessage(
                            chatId: chatId,
                            text: "<b>Выберите удобный день для вас:</b>",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                            replyMarkup: GetServicesKeyboard(),
                            cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, используйте кнопки меню 👇",
                            replyMarkup: GetTimeKeyboard(),
                            cancellationToken: cancellationToken
                            );
                        }
                    }
                }
                _logger.LogInformation($"DEBUG: Пользователь {chatId} в состоянии {userState}");
                
            }
            else
            {
                _logger.LogInformation($"DEBUG: Пользователь {chatId} в состоянии {userState}");
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Пожалуйста, используйте кнопки меню 👇",
                    replyMarkup: GetMenuKeyboard(),
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ошибка при получении обновления Telegram.Bot");
        return Task.CompletedTask;
    }

    private async Task UpdateUserState(long chatId, string state)
    {
        _logger.LogInformation($"DEBUG: смена пользователя {chatId} на состояние {state}");
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            var user = await db.users.FindAsync(chatId);

            if (user != null)
            {
                _logger.LogInformation($"Смена состояния для {chatId} на {state}");
                user.currentState = state;
            }
            else
            {
                _logger.LogWarning($"DEBUG: Пользователь {chatId} не найден. Создаю новую запись");
                user = new Models.User
                {
                    Id = chatId,
                    TelegramUserName = "Unknown",
                    currentState = state
                };
                db.users.Add(user);
            }
            await db.SaveChangesAsync();
        }
    }
    private async Task<string> GetUserState(long chatId)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            var user = await db.users.FindAsync(chatId);

            return user?.currentState ?? "None";
        }
    }
    private ReplyKeyboardMarkup GetMenuKeyboard()
    {
        var keyboard = new ReplyKeyboardMarkup(new []
        {
            new KeyboardButton[] {"📝 Записаться на услугу"},
            new KeyboardButton[] {"🗒 Моя запись"}
        })
        {
            ResizeKeyboard = true
        };
        return keyboard;
    }

    private ReplyKeyboardMarkup GetContactKeyboard()
    {
        var keyboard = new ReplyKeyboardMarkup(new []
        {
            new KeyboardButton("📱 Поделиться номером телефона") {RequestContact = true}
        })
        {
            ResizeKeyboard =true
        };
        return keyboard;
    }

    private ReplyKeyboardMarkup GetServicesKeyboard()
    {
        var keyboard = new ReplyKeyboardMarkup(new []
        {
            new KeyboardButton[] {"✂️ Мужская стрижка", "💇‍♀️ Женская стрижка"},
            new KeyboardButton[] {"🎨 Окрашивание", "💆‍♂️ Уход за волосами"},
            new KeyboardButton[] {"🔙 Вернуться в главное меню"}
        })
        {
            ResizeKeyboard = true  
        };
        return keyboard;
    }

    private ReplyKeyboardMarkup GetTimeKeyboard()
    {
        var buttons = _allowtime.Select(time => new KeyboardButton(time)).ToArray();

        var rows = new List<KeyboardButton[]>();

        for (int i = 0; i < buttons.Length; i+=2)
        {
            rows.Add(buttons.Skip(i).Take(2).ToArray());
        }

        rows.Add(new[] {new KeyboardButton("🔙 Назад")});

        return new ReplyKeyboardMarkup(rows) {ResizeKeyboard = true};
    }

    private ReplyKeyboardMarkup GetDayKeyboard()
    {
        var buttons = _daysOfWeek.Keys.Select(day => new KeyboardButton(day)).ToArray();

        var rows = new List<KeyboardButton[]>();

        for (int i =0; i < buttons.Length; i+=2)
        {
            rows.Add(buttons.Skip(i).Take(2).ToArray());
        }

        rows.Add(new[] {new KeyboardButton("🔙 Назад")});

        return new ReplyKeyboardMarkup(rows) {ResizeKeyboard = true};
    }
}