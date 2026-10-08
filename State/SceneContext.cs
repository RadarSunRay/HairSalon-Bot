using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bot.State
{
    public class SceneContext
    {
        public IBotState CurrentState { get; set; }
        public ITelegramBotClient botClient { get; private set; }
        public IServiceProvider serviceProvider { get; private set; }
        public long chatId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public SetPhoneNumberState SetPhoneNumberState { get; private set; }
        public MainMenuState MainMenuState { get; private set; }
        public WaitingForServiceState WaitingForServiceState { get; private set; }
        public WaitingForDayState WaitingForDayState { get; private set; }
        public WaitingForTimeState WaitingForTimeState { get; private set; }
        public UserProfileState UserProfileState { get; private set; }
        public SceneContext()
        {
            SetPhoneNumberState = new SetPhoneNumberState();
            MainMenuState = new MainMenuState();
            WaitingForDayState = new WaitingForDayState();
            WaitingForServiceState = new WaitingForServiceState();
            WaitingForTimeState = new WaitingForTimeState();
            UserProfileState = new UserProfileState();
        }

        public async Task ChangeState(IBotState newState)
        {
            CurrentState = newState;
            await newState.EnterHandleAsync(this);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, IServiceProvider serviceProvider, long chatId, CancellationToken cancellationToken)
        {
            this.botClient = botClient;
            this.serviceProvider = serviceProvider;
            this.chatId = chatId;
            CancellationToken = cancellationToken;
        }

        public async Task ProccessMessageAsync(Update update)
        {
            await CurrentState.HandleInputAsync(this, update);
        }
    }
}
