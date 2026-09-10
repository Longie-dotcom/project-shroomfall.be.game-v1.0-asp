using Application.Feature.Abstraction;
using Application.Feature.Connection.Command;
using Application.Interface.Realtime.Managers;
using Application.Service.WorldService;
using Domain.DomainException;
using Domain.Runtime.EntityDomain.Component;
using ResponseCode;

namespace Application.Feature.Connection.Handler
{
    public class UnloadSessionHandler : IHandler<UnloadSessionCommand>
    {
        #region Attributes
        private readonly ISessionManager sessionManager;
        private readonly RoomMigrationService roomMigrationService; 
        private readonly WorldContext worldContext;
        #endregion

        #region Properties
        #endregion

        public UnloadSessionHandler(
            ISessionManager sessionManager,
            RoomMigrationService roomMigrationService,
            WorldContext worldContext)
        {
            this.sessionManager = sessionManager;
            this.roomMigrationService = roomMigrationService;
            this.worldContext = worldContext;
        }

        #region Methods
        public async Task Handle(
            UnloadSessionCommand command)
        {
            // Resolve live gameplay session first to know which room they are occupying
            var playerInstanceId = sessionManager.Get(command.UserID);
            if (playerInstanceId == null)
                return; // Session was already entirely cleaned up

            // Validate player instance existence
            var player = worldContext.GetEntity(playerInstanceId);
            if (player == null)
                throw new InternalException(
                    ApplicationCode.ConnectionHandlerCode.UnloadSessionPlayerInstanceNotFound,
                    $"Player instance in runtime with instance ID: {playerInstanceId} is not found");

            // Validate transform existence
            var transform = player.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.ConnectionHandlerCode.UnloadSessionTransformMissing,
                    $"Player instance {playerInstanceId} is missing its TransformInstance component.");

            // Freeze engine loops and release residency
            await roomMigrationService.PlayerQuitGame(
                transform.RoomSpatialID, 
                command.UserID, 
                player);

            // Clear the overall session
            sessionManager.Remove(command.UserID);
        }
        #endregion
    }
}