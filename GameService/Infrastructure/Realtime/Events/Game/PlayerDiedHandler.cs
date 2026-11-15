using Application.Interface.Realtime;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Interface.Realtime.Managers;
using Application.Service.WorldService;
using AutoMapper;
using Contract.DTO.Feature.Connection.Response;
using Contract.DTO.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;

namespace Infrastructure.Realtime.Events.Game
{
    public class PlayerDiedHandler : IEventHandler
    {
        #region Attributes
        private readonly IMapper mapper;
        private readonly IRealtimePublisher publisher;
        private readonly IConnectionManager connectionManager;
        private readonly WorldContext worldContext;
        private readonly RoomMigrationService roomMigrationService;
        #endregion

        public PlayerDiedHandler(
            IMapper mapper,
            IRealtimePublisher publisher,
            IConnectionManager connectionManager,
            WorldContext worldContext,
            RoomMigrationService roomMigrationService)
        {
            this.mapper = mapper;
            this.publisher = publisher;
            this.connectionManager = connectionManager;
            this.worldContext = worldContext;
            this.roomMigrationService = roomMigrationService;
        }

        #region Methods
        public async Task Handle(
            IEvent @event)
        {
            if (@event is not PlayerDiedEvent diedEvent)
                return;

            var player = worldContext.GetEntity(diedEvent.EntityInstanceID);
            if (player == null)
                return;

            var ownership = player.GetComponent<OwnershipInstance>();
            if (ownership == null)
                return;

            // Respawn even when offline: LoadSession will then return the personal room on reconnect
            var room = await roomMigrationService.EnterRoomAsync(player, ownership.PersonalRoomID);

            var connectionId = connectionManager.Get(ownership.UserID);
            if (connectionId == null)
                return;

            await publisher.SendPlayerRespawned(
                connectionId,
                new SaveGameDTO()
                {
                    PlayerData = mapper.Map<EntityInstanceDTO>(player),
                    RoomData = room
                });
        }
        #endregion
    }
}
