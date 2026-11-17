using Application.Interface.Realtime.Events;
using Domain.Runtime.EntityDomain.Component;

namespace Application.Interface.Realtime.Events.Game
{
    public class QuestProgressUpdatedEvent : IEvent
    {
        #region Attributes
        #endregion

        #region Properties
        public string EntityInstanceID { get; }
        public QuestProgressInstance QuestProgressInstance { get; }
        public DateTime OccurredAt { get; }
        #endregion

        public QuestProgressUpdatedEvent(
            string entityInstanceId,
            QuestProgressInstance questProgressInstance)
        {
            EntityInstanceID = entityInstanceId;
            QuestProgressInstance = questProgressInstance;
            OccurredAt = DateTime.UtcNow;
        }

        #region Methods
        #endregion
    }
}
