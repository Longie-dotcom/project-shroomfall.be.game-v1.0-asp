using Application.Interface.Realtime;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Interface.Realtime.Managers;
using AutoMapper;
using Contract.DTO.Runtime.EntityDomain.Component;

namespace Infrastructure.Realtime.Events.Game
{
    public class QuestProgressUpdatedHandler : IEventHandler
    {
        #region Attributes
        private readonly IMapper mapper;
        private readonly IRealtimePublisher publisher;
        private readonly ISessionManager sessionManager;
        private readonly IConnectionManager connectionManager;
        #endregion

        public QuestProgressUpdatedHandler(
            IMapper mapper,
            IRealtimePublisher publisher,
            ISessionManager sessionManager,
            IConnectionManager connectionManager)
        {
            this.mapper = mapper;
            this.publisher = publisher;
            this.sessionManager = sessionManager;
            this.connectionManager = connectionManager;
        }

        #region Methods
        public async Task Handle(
            IEvent @event)
        {
            if (@event is not QuestProgressUpdatedEvent updatedEvent)
                return;

            var userId = sessionManager.GetUserIdByPlayerId(updatedEvent.EntityInstanceID);
            if (userId == null)
                return;

            var connectionId = connectionManager.Get(userId);
            if (connectionId == null)
                return;

            await publisher.SendQuestProgressUpdated(
                connectionId,
                mapper.Map<QuestProgressInstanceDTO>(updatedEvent.QuestProgressInstance));
        }
        #endregion
    }
}
