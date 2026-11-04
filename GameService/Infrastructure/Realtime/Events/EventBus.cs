using Application.Interface.Realtime.Events;

namespace Infrastructure.Realtime.Events
{
    public class EventBus : IEventBus
    {
        #region Attributes
        private List<IEvent> next = new(1024);
        private readonly object _lock = new();
        #endregion

        #region Properties
        #endregion

        public EventBus() { }

        #region Methods
        public void Publish(
            IEvent @event)
        {
            lock (_lock)
            {
                next.Add(@event);
            }
        }

        public List<IEvent> Drain()
        {
            lock (_lock)
            {
                // Hand out this tick's events directly (no one-tick delay)
                var result = next;

                next = new List<IEvent>(1024);

                return result;
            }
        }
        #endregion
    }
}