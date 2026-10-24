using Domain.Runtime.EntityDomain;
using System.Runtime.CompilerServices;

namespace Domain.Runtime.WorldDomain.Spatial
{
    public sealed class ReferenceEqualityComparer
        : IEqualityComparer<EntityInstance>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public bool Equals(
            EntityInstance? x,
            EntityInstance? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(
            EntityInstance obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    public class ChunkSpatial
    {
        #region Attributes
        private readonly Dictionary<(int x, int y), HashSet<EntityInstance>> entities = new();
        #endregion

        #region Properties
        #endregion

        public ChunkSpatial() { }

        #region Methods
        public void AddEntity(
            EntityInstance entity,
            int x,
            int y)
        {
            var key = (x, y);

            if (!entities.TryGetValue(key, out var set))
            {
                set = new HashSet<EntityInstance>(ReferenceEqualityComparer.Instance);
                entities[key] = set;
            }

            set.Add(entity);
        }

        public void RemoveEntity(
            EntityInstance entity, 
            int x, 
            int y)
        {
            var key = (x, y);

            if (entities.TryGetValue(key, out var set))
            {
                set.Remove(entity);

                if (set.Count == 0)
                    entities.Remove(key);
            }
        }

        public IEnumerable<EntityInstance> Query(
            int x, int y)
        {
            var key = (x, y);

            if (!entities.TryGetValue(key, out var set))
                return Enumerable.Empty<EntityInstance>();

            return set;
        }
        #endregion
    }
}