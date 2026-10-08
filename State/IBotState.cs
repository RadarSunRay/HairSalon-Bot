using Telegram.Bot.Types;

namespace Bot.State
{
    public interface IBotState
    {
        public Task EnterHandleAsync(SceneContext sceneContext);
        public Task HandleInputAsync(SceneContext sceneContext, Update update);
    }
}
