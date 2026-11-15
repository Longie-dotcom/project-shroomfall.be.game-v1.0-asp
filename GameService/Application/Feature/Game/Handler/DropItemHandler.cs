using Application.Feature.Abstraction;
using Application.Feature.Game.Command;
using Application.Interface.Realtime.Managers;
using Application.Service.EntityService;
using Application.Service.WorldService;
using Application.Service.WorldService.Creation;
using Domain.DomainException;
using Domain.Runtime.EntityDomain.Component;
using ResponseCode;

namespace Application.Feature.Game.Handler
{
    public class DropItemHandler : IHandler<DropItemCommand>
    {
        #region Attributes
        private readonly WorldContext worldContext;
        private readonly ISessionManager sessionManager;
        private readonly InventoryService inventoryService;
        private readonly EntitySpawnService entitySpawnService;
        #endregion

        #region Properties
        #endregion

        public DropItemHandler(
            WorldContext worldContext,
            ISessionManager sessionManager,
            InventoryService inventoryService,
            EntitySpawnService entitySpawnService)
        {
            this.worldContext = worldContext;
            this.sessionManager = sessionManager;
            this.inventoryService = inventoryService;
            this.entitySpawnService = entitySpawnService;
        }

        #region Methods
        public Task Handle(
            DropItemCommand command)
        {
            var dto = command.DTO;

            // Validate session existence
            var playerInstanceId = sessionManager.Get(command.UserID);
            if (string.IsNullOrWhiteSpace(playerInstanceId))
                throw new Unauthorized(
                    ApplicationCode.GameHandlerCode.DropItemSessionNotFound,
                    $"User with user ID: {command.UserID} has no session");

            // Validate player instance existence
            var player = worldContext.GetEntity(playerInstanceId);
            if (player == null)
                throw new BadRequest(
                    ApplicationCode.GameHandlerCode.DropItemPlayerInstanceNotFound,
                    $"User with user ID: {command.UserID} has no player instance");

            var transform = player.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.GameHandlerCode.DropItemTransformMissing,
                    $"Player {player.ID} has no TransformInstance when dropping.");

            // Remove from inventory, then spawn as a world item at the player's feet
            var item = inventoryService.DropItem(player, dto.ItemInstanceID);

            entitySpawnService.Spawn(
                new WorldItemCreateContext(
                    Guid.NewGuid().ToString(),
                    transform.RoomSpatialID,
                    transform.LayerZ,
                    transform.Position,
                    item));

            return Task.CompletedTask;
        }
        #endregion
    }
}
