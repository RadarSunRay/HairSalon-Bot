using Bot.Data;
using Bot.Models;
using Bot.State;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace Bot.Services;
public class TelegramBotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _botclient;
    private readonly ILogger<TelegramBotBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Context _context;
    public TelegramBotBackgroundService(ITelegramBotClient botClient,
    ILogger<TelegramBotBackgroundService> logger, IServiceProvider serviceProvider, Context context)
    {
        _botclient = botClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _context = context;
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
        long? chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;

        if (chatId == null) return;

        var context = _context.GetContext(chatId.Value);
        await context.HandleUpdateAsync(botClient, _serviceProvider, chatId.Value, cancellationToken);

        if (update.Message is { Text: { } text } && text.StartsWith("/start"))
        {

            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                var user = await userService.CreateUserAsync(context, update.Message.From?.Username ?? "No name");

                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await context.ChangeState(context.SetPhoneNumberState);
                }
                else
                    await context.ChangeState(context.MainMenuState);
            }
        }

        await context.ProccessMessageAsync(update);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ошибка при получении обновления Telegram.Bot");
        return Task.CompletedTask;
    }
}