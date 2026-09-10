using Application.Interface.Cache;
using Application.Interface.Cache.EntityDomain;
using Application.Interface.Cache.EntityDomain.Component;
using Application.Interface.Cache.LocalizationDomain;
using Application.Interface.Cache.MetaDomain;
using Application.Interface.Cache.WorldDomain;
using Application.Interface.Utility;
using Contract.DTO.Messaging;
using Domain.DomainException;
using ResponseCode;

namespace Infrastructure.Cache
{
    public class CacheProvider : ICacheProvider
    {
        #region Attributes
        private readonly ITelemetryQueue telemetryQueue;

        private readonly IAICache aiCache;
        private readonly IAppearanceCache appearanceCache;
        private readonly ICollisionCache collisionCache;
        private readonly ICharacteristicCache characteristicCache;
        private readonly IInventoryCache inventoryCache;
        private readonly ILifetimeCache lifetimeCache;
        private readonly IProjectileCache projectileCache;
        private readonly ITriggeredEffectCache triggeredEffectCache;
        private readonly IEntityCache entityCache;
        private readonly ILocaleCache localeCache;
        private readonly IEffectCache effectCache;
        private readonly IItemCache itemCache;
        private readonly IRoomCache roomCache;
        #endregion

        #region Properties
        public IAICache AI => aiCache;
        public IAppearanceCache Appearance => appearanceCache;
        public ICollisionCache Collision => collisionCache;
        public ICharacteristicCache Characteristic => characteristicCache;
        public IInventoryCache Inventory => inventoryCache;
        public ILifetimeCache Lifetime => lifetimeCache;
        public IProjectileCache Projectile => projectileCache;
        public ITriggeredEffectCache TriggeredEffect => triggeredEffectCache;
        public IEntityCache Entity => entityCache;
        public ILocaleCache Locale => localeCache;
        public IEffectCache Effect => effectCache;
        public IItemCache Item => itemCache;
        public IRoomCache Room => roomCache;
        #endregion

        public CacheProvider(
            ITelemetryQueue telemetryQueue,

            IAICache aiCache,
            IAppearanceCache appearanceCache,
            ICollisionCache collisionCache,
            ICharacteristicCache characteristicCache,
            IInventoryCache inventoryCache,
            ILifetimeCache lifetimeCache,
            IProjectileCache projectileCache,
            ITriggeredEffectCache triggeredEffectCache,
            IEntityCache entityCache,
            ILocaleCache localeCache,
            IEffectCache effectCache,
            IItemCache itemCache,
            IRoomCache roomCache)
        {
            this.telemetryQueue = telemetryQueue;

            this.aiCache = aiCache;
            this.appearanceCache = appearanceCache;
            this.collisionCache = collisionCache;
            this.characteristicCache = characteristicCache;
            this.inventoryCache = inventoryCache;
            this.lifetimeCache = lifetimeCache;
            this.projectileCache = projectileCache;
            this.triggeredEffectCache = triggeredEffectCache;
            this.entityCache = entityCache;
            this.localeCache = localeCache;
            this.effectCache = effectCache;
            this.itemCache = itemCache;
            this.roomCache = roomCache;
        }

        #region Methods
        public async Task LoadAllAsync(
            DefinitionCacheDTO dto)
        {
            try
            {
                // Hydrate caches
                aiCache.Load(dto.AIs);
                appearanceCache.Load(dto.Appearances);
                collisionCache.Load(dto.Collisions);
                characteristicCache.Load(dto.Characteristics);
                inventoryCache.Load(dto.Inventories);
                lifetimeCache.Load(dto.Lifetimes);
                projectileCache.Load(dto.Projectiles);
                triggeredEffectCache.Load(dto.TriggeredEffects);
                entityCache.Load(dto.Entities);
                localeCache.Load(dto.Locales);
                effectCache.Load(dto.Effects);
                itemCache.Load(dto.Items);
                roomCache.Load(dto.Rooms, dto.Cells, dto.EntitySpawnRules);

                telemetryQueue.EnqueueAlert(
                    InfrastructureCode.CacheProviderCode.LoadSuccess,
                    "Global caches successfully hydrated with metadata definitions.",
                    TelemetrySeverity.Info);
            }
            catch (Exception ex) when (ex is not InternalException)
            {
                throw new InternalException(
                    InfrastructureCode.CacheProviderCode.LoadFailed,
                    $"Critical failure occurred during global cache hydration (LoadAllAsync): {ex.Message}");
            }
        }
        #endregion
    }
}