using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Service.EntityService;
using Application.Service.MetaService;
using Application.Service.WorldService;
using Application.Service.WorldService.Creation;
using Application.System.Queue;
using Contract;
using Domain.Runtime.EntityDomain.Component;

namespace Application.System.System
{
    public class EntityTrigger
    {
        #region Attributes
        private readonly WorldContext worldContext;
        private readonly IEventBus eventBus;
        private readonly ItemService itemService;
        private readonly ProjectileService projectileService;
        private readonly TriggeredEffectService triggeredEffectService;
        private readonly EntitySpawnService entitySpawnService;
        private readonly InventoryService inventoryService;
        #endregion

        public EntityTrigger(
            WorldContext worldContext,
            IEventBus eventBus,
            ItemService itemService,
            ProjectileService projectileService,
            TriggeredEffectService triggeredEffectService,
            EntitySpawnService entitySpawnService,
            InventoryService inventoryService)
        {
            this.worldContext = worldContext;
            this.eventBus = eventBus;
            this.itemService = itemService;
            this.projectileService = projectileService;
            this.triggeredEffectService = triggeredEffectService;
            this.entitySpawnService = entitySpawnService;
            this.inventoryService = inventoryService;
        }

        #region Methods
        public void Apply(
            CommandBuffer commandBuffer)
        {
            while (commandBuffer.Results.TryDequeue(out var result))
            {
                switch (result)
                {
                    case MovementResult movementRes:
                        ApplyMovement(movementRes, commandBuffer);
                        break;

                    case ItemActionResult itemActionRes:
                        ApplyItemAction(itemActionRes);
                        break;

                    case EntityExpiredResult entityExpiredRes:
                        ApplyEntityExpired(entityExpiredRes, commandBuffer);
                        break;

                    case VitalThresholdResult vitalThresholdRes:
                        ApplyVitalThreshold(vitalThresholdRes, commandBuffer);
                        break;

                    case EntityDespawnResult entityDespawnRes:
                        ApplyEntityDespawn(entityDespawnRes);
                        break;
                }
            }
        }

        private void ApplyMovement(
            MovementResult result,
            CommandBuffer commandBuffer)
        {
            var entity = worldContext.GetEntity(result.EntityInstanceID);
            if (entity == null)
                return;

            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
                return;

            // Apply authoritative movement
            worldContext.EntityMove(
                entity.ID,
                result.FinalPosition,
                result.LayerZ);

            // Publish active movement position update
            if (transform.PositionChangedThisFrame)
            {
                eventBus.Publish(new EntityActedEvent(
                    entity.ID,
                    transform.RoomSpatialID,
                    transform.Position,
                    transform.FacingDirection,
                    transform.CurrentAction,
                    null
                ));
            }

            foreach (var touched in result.TriggeredEntities)
            {
                triggeredEffectService.OnEntityTouched(touched, entity);

                if (projectileService.TryHandleImpact(entity))
                {
                    commandBuffer.Commands.Enqueue(
                        new EntityDespawnCommand(entity.ID, false));
                }

                if (inventoryService.TryPickItem(entity, touched))
                {
                    commandBuffer.Commands.Enqueue(
                        new EntityDespawnCommand(touched.ID, false));
                }
            }
        }

        private void ApplyItemAction(
            ItemActionResult result)
        {
            var entity = worldContext.GetEntity(result.EntityInstanceID);
            if (entity == null) 
                return;

            // Execute the item usage logic
            itemService.Execute(entity, result.Context);

            // Grab the transform to get current position/facing direction
            var transform = entity.GetComponent<TransformInstance>();
            if (transform != null && result.Context.ItemDef.TriggeredAction.HasValue)
            {
                transform.ExecuteAction(
                    result.Context.ItemDef.TriggeredAction.Value,
                    result.Context.ItemDef.Id,
                    Constraint.LOCK_ACTION_DURATION);
            }
        }

        private void ApplyEntityExpired(
            EntityExpiredResult result,
            CommandBuffer commandBuffer)
        {
            var entity = worldContext.GetEntity(result.EntityInstanceID);
            if (entity == null) 
                return;

            // Apply projectile logic
            projectileService.TryHandleImpact(entity);

            commandBuffer.Commands.Enqueue(new EntityDespawnCommand(entity.ID, false));
        }

        private void ApplyVitalThreshold(
            VitalThresholdResult result,
            CommandBuffer commandBuffer)
        {
            switch (result.Outcome)
            {
                case DeathOutcome.Entity:
                    commandBuffer.Commands.Enqueue(new EntityDespawnCommand(result.EntityInstanceID, true));
                    break;

                case DeathOutcome.Player:
                    // TODO: Return the player to the their private room and lost their 80% money + 4 random items
                    break; 

                case DeathOutcome.None:
                default:
                    break;
            }
        }

        private void ApplyEntityDespawn(
            EntityDespawnResult result)
        {
            var entity = worldContext.GetEntity(result.EntityInstanceID);
            if (entity == null)
                return;

            if (result.TriggerDeathLogic)
            {
                var transform = entity.GetComponent<TransformInstance>();

                if (transform != null)
                {
                    var drops = inventoryService.DropAllItems(entity);

                    foreach (var item in drops)
                    {
                        entitySpawnService.Spawn(
                            new WorldItemCreateContext(
                                Guid.NewGuid().ToString(),
                                transform.RoomSpatialID,
                                transform.LayerZ,
                                transform.Position,
                                item));
                    }
                }
            }

            entitySpawnService.Despawn(entity);
        }
        #endregion
    }
}