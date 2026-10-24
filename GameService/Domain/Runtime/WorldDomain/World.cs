using Contract.Common;
using Domain.Abstraction.World;
using Domain.DomainException;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.WorldDomain.Spatial;
using ResponseCode;

namespace Domain.Runtime.WorldDomain
{
    public class World : IWorldQuery, IEntityCommand, IRoomCommand
    {
        #region Attributes
        private readonly Dictionary<string, EntityInstance> entities;
        private readonly Dictionary<string, RoomSpatial> rooms;
        #endregion

        #region Properties
        #endregion

        public World()
        {
            entities = new Dictionary<string, EntityInstance>();
            rooms = new Dictionary<string, RoomSpatial>();
        }

        #region Query
        public IEnumerable<EntityInstance> GetEntities()
        {
            return entities.Values;
        }

        public EntityInstance? GetEntity(
            string entityInstanceId)
        {
            if (!entities.TryGetValue(entityInstanceId, out var entity))
                return null;

            return entity;
        }

        public (RoomSpatial?, IEnumerable<EntityInstance>) QuerySpatial(
            string roomSpatialId,
            int x, int y, int z)
        {
            return rooms.TryGetValue(roomSpatialId, out var roomSpatial)
                ? (roomSpatial, roomSpatial.Query(x, y, z))
                : (null, new List<EntityInstance>());
        }

        public IEnumerable<RoomSpatial> GetRooms()
        {
            return rooms.Values;
        }

        public RoomSpatial? GetRoom(
            string roomSpatialId)
        {
            return rooms.TryGetValue(roomSpatialId, out var roomSpatial)
                ? roomSpatial
                : null;
        }

        public RoomSpatial? GetRoomByOwner(
            string ownerId)
        {
            return rooms.Values.FirstOrDefault(r => r.OwnerID == ownerId);
        }
        #endregion

        #region Command
        public void AddEntity(
            EntityInstance entityInstance)
        {
            var transform = entityInstance.GetComponent<TransformInstance>();
            if (transform == null) return;

            // Add or update existed entity
            entities[entityInstance.ID] = entityInstance;

            // Register in spatial index
            IndexEntity(transform);
        }

        public void RemoveEntity(
            string entityInstanceId)
        {
            if (!entities.TryGetValue(entityInstanceId, out var entityInstance))
                return;

            var transform = entityInstance.GetComponent<TransformInstance>();
            if (transform == null) return;

            // Remove from spatial first
            DeindexEntity(transform);

            // Remove entity from dictionary
            entities.Remove(entityInstanceId);

            return;
        }

        public void EntityMove(
            string entityInstanceId, 
            Vector2 newPosition, 
            int layerZ)
        {
            if (!entities.TryGetValue(entityInstanceId, out var entityInstance))
                throw new InternalException(
                    DomainCode.WorldCode.EntityInstanceNotFoundOnMoved,
                    $"Entity instance with instance ID: {entityInstanceId} not found when move");

            var transform = entityInstance.GetComponent<TransformInstance>();
            if (transform == null) return;

            // Capture OLD derived state
            var oldKey = transform.GetSpatialKey();

            // Mutate authoritative state
            transform.SetPosition(newPosition, layerZ);

            // Spatial sync
            IndexEntityMove(transform, oldKey);
        }

        public void ChangeRoom(
            string entityInstanceId,
            Vector2 newPosition,
            int layerZ,
            string newRoomSpatialId)
        {
            if (!entities.TryGetValue(entityInstanceId, out var entityInstance))
                throw new InternalException(
                    DomainCode.WorldCode.EntityInstanceNotFoundOnRoomChanged,
                    $"Entity instance with instance ID: {entityInstanceId} not found when changed room");

            var transform = entityInstance.GetComponent<TransformInstance>();
            if (transform == null) return;

            // Remove from OLD room spatial index
            DeindexEntity(transform);

            // Mutate authoritative state
            transform.ChangeRoom(
                newRoomSpatialId,
                newPosition,
                layerZ);

            // Register into NEW room spatial index
            IndexEntity(transform);
        }

        public void AddRoom(
            RoomSpatial roomSpatial)
        {
            rooms[roomSpatial.ID] = roomSpatial;
        }

        public void RemoveRoom(
            string roomSpatialId)
        {
            rooms.Remove(roomSpatialId);
        }

        private void IndexEntity(
            TransformInstance transform)
        {
            if (!rooms.TryGetValue(transform.RoomSpatialID, out var room))
                throw new InternalException(
                    DomainCode.WorldCode.RoomSpatialNotFoundOnEntityAdded,
                    $"Room spatial not registered: {transform.RoomSpatialID}, when entity: {transform.Entity.ID} was added");

            var key = transform.GetSpatialKey();
            room.AddEntity(transform.Entity, key.cx, key.cy, key.x, key.y, key.z);
        }

        private void DeindexEntity(
            TransformInstance transform)
        {
            if (!rooms.TryGetValue(transform.RoomSpatialID, out var room))
                return;

            var key = transform.GetSpatialKey();
            room.RemoveEntity(transform.Entity, key.cx, key.cy, key.x, key.y, key.z);
        }

        private void IndexEntityMove(
            TransformInstance transform,
            (int cx, int cy, int x, int y, int z) oldKey)
        {
            if (!rooms.TryGetValue(transform.RoomSpatialID, out var room))
                throw new InternalException(
                    DomainCode.WorldCode.RoomSpatialNotFoundOnEntityMoved,
                    $"Room spatial not registered: {transform.RoomSpatialID}, when entity: {transform.Entity.ID} moved");

            var newKey = transform.GetSpatialKey();
            if (oldKey == newKey) return;

            room.EntityMove(transform.Entity, oldKey, newKey);
        }
        #endregion
    }
}