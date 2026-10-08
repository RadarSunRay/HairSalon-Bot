using System.Collections.Concurrent;

namespace Bot.State
{
    public class Context
    {
        private readonly ConcurrentDictionary<long, SceneContext> _context = new();

        public SceneContext GetContext(long userId)
        {
            return _context.GetOrAdd(userId, id => new SceneContext());
        }
    }
}
