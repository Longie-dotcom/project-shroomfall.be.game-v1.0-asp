using Application.Interface.Cache;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Interface.Utility;
using Application.Service.EntityService;
using Application.Service.WorldService.Factory;
using Application.System.Abstraction;
using Application.System.Queue;
using Contract;
using Contract.Common;
using Contract.DTO.Definition.WorldDomain;
using Contract.Enum.WorldDomain;
using Domain.DomainException;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using ResponseCode;

namespace Application.Service.WorldService.Creation
{
    public class WorldEntityCreateContext
    {
        public string InstanceID { get; }
        public string DefinitionID { get; }
        public string RoomSpatialID { get; }
        public int LayerZ { get; }
        public Vector2 Position { get; }

        public WorldEntityCreateContext(
            string instanceId,
            string definitionId,
            string roomSpatialId,
            int layerZ,
            Vector2 position)
        {
            InstanceID = instanceId;
            DefinitionID = definitionId;
            RoomSpatialID = roomSpatialId;
            LayerZ = layerZ;
            Position = position;
        }
    }

    public class ProjectileEntityCreateContext : WorldEntityCreateContext
    {
        public Vector2 Direction { get; }
        public string SourceEntityID { get; }

        public ProjectileEntityCreateContext(
            string instanceId,
            string definitionId,
            string roomSpatialId,
            int layerZ,
            Vector2 position,
            Vector2 direction,
            string sourceEntityId) : base(
                instanceId, 
                definitionId,
                roomSpatialId, 
                layerZ,
                position)
        {
            Direction = direction;
            SourceEntityID = sourceEntityId;
        }
    }

    public class WorldItemCreateContext : WorldEntityCreateContext
    {
        public ItemInstance Payload { get; }

        public WorldItemCreateContext(
            string instanceId,
            string roomSpatialId,
            int layerZ,
            Vector2 position,
            ItemInstance payload) : base(
                instanceId, 
                Constraint.DEFAULT_ENTITY_ITEM_DEFINITION_ID,
                roomSpatialId, 
                layerZ, 
                position)
        {
            Payload = payload;
        }
    }

    public class PlayerEntityCreateContext : WorldEntityCreateContext
    {
        public string UserID { get; }
        public string PersonalRoomID { get; }

        public PlayerEntityCreateContext(
            string instanceId,
            string definitionId,
            string roomSpatialId,
            int layerZ,
            Vector2 position,
            string userId,
            string personalRoomID) : base(
                instanceId,
                definitionId,
                roomSpatialId,
                layerZ,
                position)
        {
            UserID = userId;
            PersonalRoomID = personalRoomID;
        }
    }

    public class EntitySpawnService : ITickService
    {
        #region Attributes
        private const float POPULATION_TICK_PER_SECOND = 10f;
        private const int SPAWN_CELL_ATTEMPTS = 5;

        private readonly IEventBus eventBus;
        private readonly WorldContext worldContext;
        private readonly EntityInstanceFactory entityInstanceFactory;
        private readonly ICacheProvider cacheProvider;
        private readonly CollisionService collisionService;
        private readonly ITelemetryQueue telemetryQueue;

        private float populationAccumulator;
        #endregion

        #region Properties
        #endregion

        public EntitySpawnService(
            IEventBus eventBus,
            WorldContext worldContext,
            EntityInstanceFactory entityInstanceFactory,
            ICacheProvider cacheProvider,
            CollisionService collisionService,
            ITelemetryQueue telemetryQueue)
        {
            this.eventBus = eventBus;
            this.worldContext = worldContext;
            this.entityInstanceFactory = entityInstanceFactory;
            this.cacheProvider = cacheProvider;
            this.collisionService = collisionService;
            this.telemetryQueue = telemetryQueue;
        }

        #region Methods
        public void Tick(
            float dt,
            CommandBuffer commandBuffer)
        {
            populationAccumulator += dt;
            if (populationAccumulator < POPULATION_TICK_PER_SECOND)
                return;

            populationAccumulator = 0;

            // shortcut: counts by (room, definition), two environment rules with the same definition in one room share a count; tag entities with SpawnRuleID if that happens
            var population = worldContext.GetEntities()
                .GroupBy(e => (e.GetComponent<TransformInstance>()?.RoomSpatialID, e.DefinitionID))
                .ToDictionary(g => g.Key, g => g.Count());

            // Loaded rooms only, cold rooms keep their snapshot population until reloaded
            foreach (var room in worldContext.GetRooms().ToList())
            {
                foreach (var rule in cacheProvider.Room.GetSpawnRules(room.DefinitionID))
                {
                    if (rule.Type != SpawnRuleType.Environment)
                        continue;

                    population.TryGetValue((room.ID, rule.EntityDefinitionID), out var alive);
                    if (alive >= rule.MinCount)
                        continue;

                    // One refill per rule per check, so the population trickles back instead of popping in at once
                    try
                    {
                        var (position, layerZ) = ResolveRulePosition(room.DefinitionID, rule);

                        Spawn(
                            new WorldEntityCreateContext(
                                $"{Guid.NewGuid()}_{rule.EntityDefinitionID}",
                                rule.EntityDefinitionID,
                                room.ID,
                                layerZ,
                                position),
                            resolvePosition: true);
                    }
                    catch (InternalException ex)
                    {
                        telemetryQueue.EnqueueAlert(
                            ex.Code,
                            ex.Message,
                            TelemetrySeverity.Error);
                    }
                }
            }
        }

        public (Vector2 position, int layerZ) ResolveRulePosition(
            string roomDefinitionId,
            EntitySpawnRuleDTO rule)
        {
            // Spawn boxes often include walls or gaps, so try a few random cells before giving up
            for (int i = 0; i < SPAWN_CELL_ATTEMPTS; i++)
            {
                int x = Random.Shared.Next(rule.MinX, rule.MaxX + 1);
                int y = Random.Shared.Next(rule.MinY, rule.MaxY + 1);

                var cell = cacheProvider.Room.GetTopCell(roomDefinitionId, x, y);
                if (cell != null)
                    return (new Vector2(x, y), cell.Z);
            }

            throw new InternalException(
                ApplicationCode.EntitySpawnServiceCode.NoSpawnCellFound,
                $"No valid spawn cell found in {SPAWN_CELL_ATTEMPTS} attempts for rule '{rule.ID}' inside room blueprint '{roomDefinitionId}'.");
        }

        public void Spawn(
            WorldEntityCreateContext context,
            bool resolvePosition = false)
        {
            var entity = entityInstanceFactory.Create(context);
            if (entity == null)
                throw new InternalException(
                    ApplicationCode.EntitySpawnServiceCode.SpawnEntityCreationFailed,
                    $"Failed to create entity instance from definition ID: {context.DefinitionID}");

            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.EntitySpawnServiceCode.ActivateTransformMissing,
                    $"Cannot activate entity {entity.ID} without a Transform component.");

            // Snap to the nearest free spot so refills never overlap live entities
            var collision = entity.GetComponent<CollisionInstance>();
            var room = worldContext.GetRoom(context.RoomSpatialID);
            if (resolvePosition && collision != null && room != null)
            {
                collisionService.SpawnAtNearestValidPosition(
                    new CollisionBody(
                        entity.ID,
                        room.ID,
                        transform.Position,
                        collision.CollisionOffset,
                        transform.LayerZ,
                        collision.CollisionShape,
                        collision.Layer,
                        collision.Mask),
                    transform,
                    room.DefinitionID);
            }

            // Inject into spatial partitioning and engine ticks
            worldContext.AddEntity(entity);

            // Inform netcode to render the instance for clients in range
            Vector2? direction = null;
            if (context is ProjectileEntityCreateContext projectileContext)
                direction = projectileContext.Direction;

            eventBus.Publish(new EntityLifecycleEvent(
                entity,
                direction,
                transform.RoomSpatialID,
                EntityLifecycleType.Spawn));
        }

        public void Despawn(
            EntityInstance entity)
        {
            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null) return;

            // Strip out from spatial partitioning and physics
            worldContext.RemoveEntity(entity.ID);

            // Inform netcode to tell clients to delete their local visual actor
            eventBus.Publish(new EntityLifecycleEvent(
                entity,
                null,
                transform.RoomSpatialID,
                EntityLifecycleType.Despawn));
        }

        public void TransitionRoom(
            EntityInstance entity,
            string targetRoomSpatialId,
            Vector2 targetPosition,
            int targetLayerZ)
        {
            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.EntitySpawnServiceCode.TransitionTransformMissing,
                    $"Cannot transition entity {entity.ID} without a Transform component.");

            var oldRoomSpatialId = transform.RoomSpatialID;

            // FAREWELL PACKET (Old Room channel)
            // Alert current nearby clients using the pre-mutation location state properties
            eventBus.Publish(new EntityLifecycleEvent(
                entity,
                null,
                oldRoomSpatialId,
                EntityLifecycleType.Despawn));

            // ATOMIC DOMAIN ROOM TRANSITION
            // Delegates dictionary indexing mutations cleanly straight into World spatial boundaries
            worldContext.ChangeRoom(
                entity.ID,
                targetPosition,
                targetLayerZ,
                targetRoomSpatialId);

            // HELLO PACKET (New Room channel)
            // Alert newly targeted zone observers using the updated spatial coordinates
            eventBus.Publish(new EntityLifecycleEvent(
                entity,
                null,
                targetRoomSpatialId,
                EntityLifecycleType.Spawn));
        }
        #endregion
    }
}