using Application.Interface.Cache;
using Application.Service.EntityService;
using Application.Service.WorldService.Factory;
using Contract.Common;
using Contract.DTO.Definition.WorldDomain;
using Contract.Enum.WorldDomain;
using Domain.DomainException;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.WorldDomain.Spatial;
using ResponseCode;

namespace Application.Service.WorldService.Creation
{
    public class RoomInstance
    {
        public RoomSpatial Room { get; set; } = default!;
        public List<EntityInstance> Entities { get; set; } = new List<EntityInstance>();
    }

    public class InitializationService
    {
        #region Attributes
        private readonly Random random;
        private readonly ICacheProvider cacheProvider;
        private readonly CollisionService collisionService;
        private readonly EntityInstanceFactory entityInstanceFactory;
        private readonly RoomSpatialFactory roomSpatialFactory;
        private readonly ResidencyService residencyService;
        #endregion

        #region Properties
        #endregion

        public InitializationService(
            ICacheProvider cacheProvider,
            CollisionService collisionService,
            EntityInstanceFactory entityInstanceFactory,
            RoomSpatialFactory roomSpatialFactory,
            ResidencyService residencyService)
        {
            random = new Random();
            this.cacheProvider = cacheProvider;
            this.collisionService = collisionService;
            this.entityInstanceFactory = entityInstanceFactory;
            this.roomSpatialFactory = roomSpatialFactory;
            this.residencyService = residencyService;
        }

        #region Methods
        public void InitializeRoom(
            string roomDefinitionId,
            string roomSpatialId,
            RoomLifecyclePolicy lifecyclePolicy,
            string? userId = null)
        {
            // Spawn Entities
            var pendingEntities = new List<EntityInstance>();
            var ownerId = SpawnEntities(roomDefinitionId, roomSpatialId, pendingEntities, userId);

            // Create Room
            var room = roomSpatialFactory.Create(roomDefinitionId, roomSpatialId, ownerId);

            // Register creation on RAM
            residencyService.RegisterRuntimeRoom(
                new RoomInstance { Room = room, Entities = pendingEntities },
                lifecyclePolicy);
        }

        private string? SpawnEntities(
            string roomDefId,
            string roomSpatialId,
            List<EntityInstance> buffer,
            string? userId = null)
        {
            var roomDef = cacheProvider.Room.Get(roomDefId);
            if (roomDef == null)
                throw new InternalException(
                    ApplicationCode.InitializationServiceCode.RoomDefinitionNotFound,
                    $"Room generation aborted. Master definition blueprint for ID '{roomDefId}' could not be loaded from store.");

            string? ownerId = null;

            var rules = cacheProvider.Room.GetSpawnRules(roomDefId);
            foreach (var rule in rules)
            {
                // Prevent spawning players if no userId is provided, or if already spawned one
                if (rule.Type == SpawnRuleType.Player && (string.IsNullOrEmpty(userId) || ownerId != null))
                    continue;

                // Enforce exactly 1 spawn for Players, regardless of what the blueprint says.
                int count = rule.Type == SpawnRuleType.Player ? 1 : random.Next(rule.MinCount, rule.MaxCount + 1);

                for (int i = 0; i < count; i++)
                {
                    // Resolve the spawned position
                    var (pos, layerZ) = ResolveSpawnPosition(roomDef, rule);

                    // Checking spawn type to create context
                    WorldEntityCreateContext context;
                    if (rule.Type == SpawnRuleType.Player && !string.IsNullOrEmpty(userId))
                    {
                        ownerId = $"{userId}_{Guid.NewGuid()}_{rule.EntityDefinitionID}";
                        context = new PlayerEntityCreateContext(
                            ownerId, 
                            rule.EntityDefinitionID,
                            roomSpatialId,
                            layerZ,
                            pos, 
                            userId, 
                            roomSpatialId);
                    }
                    else
                    {
                        context = new WorldEntityCreateContext(
                            $"{Guid.NewGuid()}_{rule.EntityDefinitionID}",
                            rule.EntityDefinitionID,
                            roomSpatialId,
                            layerZ, 
                            pos);
                    }

                    // Create entity based on context and validate spawn position
                    var entity = entityInstanceFactory.Create(context);

                    var transform = entity.GetComponent<TransformInstance>();
                    if (transform == null)
                        throw new InternalException(
                            ApplicationCode.InitializationServiceCode.TransformComponentMissing,
                            $"Spawning failed. Entity blueprint '{rule.EntityDefinitionID}' lacks a required TransformInstance component.");

                    var collision = entity.GetComponent<CollisionInstance>();
                    if (collision == null)
                        throw new InternalException(
                            ApplicationCode.InitializationServiceCode.CollisionComponentMissing,
                            $"Spawning failed. Entity blueprint '{rule.EntityDefinitionID}' lacks a required CollisionInstance component.");

                    var spawnBody = new CollisionBody(
                        entity.ID,
                        roomSpatialId,
                        pos,
                        collision.CollisionOffset,
                        layerZ,
                        collision.CollisionShape,
                        collision.Layer,
                        collision.Mask);

                    collisionService.SpawnAtNearestValidPosition(
                        spawnBody,
                        transform,
                        roomDef.Id,
                        buffer,
                        5);

                    buffer.Add(entity);
                }
            }

            return ownerId;
        }

        private (Vector2 position, int layerZ) ResolveSpawnPosition(
            RoomDefinitionDTO roomDef,
            EntitySpawnRuleDTO rule)
        {
            // Pick a random coordinate within the rule's boundaries
            int x = random.Next(rule.MinX, rule.MaxX + 1);
            int y = random.Next(rule.MinY, rule.MaxY + 1);

            // Get top spawn cell
            var cell = cacheProvider.Room.GetTopCell(roomDef.Id, x, y);
            if (cell == null)
                throw new InternalException(
                    ApplicationCode.InitializationServiceCode.NoSpawnCellFound,
                    $"Coordinate resolution failed. No valid cell architecture found at grid location ({x}, {y}) inside Room Blueprint '{roomDef.Id}'.");

            return (new Vector2(x, y), cell.Z);
        }
        #endregion
    }
}