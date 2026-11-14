using Application.Interface.Realtime;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Interface.Realtime.Managers;
using Contract.DTO.Feature.Game.Response;

namespace Infrastructure.Realtime.Events.Game
{
    public class EconomicUpdatedHandler : IEventHandler
    {
        #region Attributes
        private readonly IRealtimePublisher publisher;
        private readonly ISessionManager sessionManager;
        private readonly IConnectionManager connectionManager;
        #endregion

        public EconomicUpdatedHandler(
            IRealtimePublisher publisher,
            ISessionManager sessionManager,
            IConnectionManager connectionManager)
        {
            this.publisher = publisher;
            this.sessionManager = sessionManager;
            this.connectionManager = connectionManager;
        }

        #region Methods
        public async Task Handle(
            IEvent @event)
        {
            if (@event is not EconomicUpdatedEvent updatedEvent)
                return;

            var userId = sessionManager.GetUserIdByPlayerId(updatedEvent.EntityInstanceID);
            if (userId == null)
                return;

            var connectionId = connectionManager.Get(userId);
            if (connectionId == null)
                return;

            await publisher.SendEconomicUpdated(
                connectionId,
                new EconomicUpdateDTO()
                {
                    CurrentMoney = updatedEvent.CurrentMoney
                });
        }
        #endregion
    }
}
