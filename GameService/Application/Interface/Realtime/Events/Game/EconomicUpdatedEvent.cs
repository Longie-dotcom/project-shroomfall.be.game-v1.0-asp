using Application.Interface.Realtime.Events;

namespace Application.Interface.Realtime.Events.Game
{
    public class EconomicUpdatedEvent : IEvent
    {
        #region Attributes
        #endregion

        #region Properties
        public string EntityInstanceID { get; }
        public int CurrentMoney { get; }
        public DateTime OccurredAt { get; }
        #endregion

        public EconomicUpdatedEvent(
            string entityInstanceId,
            int currentMoney)
        {
            EntityInstanceID = entityInstanceId;
            CurrentMoney = currentMoney;
            OccurredAt = DateTime.UtcNow;
        }

        #region Methods
        #endregion
    }
}
