using Application.Interface.Cache.EntityDomain;
using Application.Interface.Cache.EntityDomain.Component;
using Application.Interface.Cache.LocalizationDomain;
using Application.Interface.Cache.MetaDomain;
using Application.Interface.Cache.WorldDomain;
using Contract.DTO.Messaging;

namespace Application.Interface.Cache
{
    public interface ICacheProvider
    {
        Task LoadAllAsync(
            DefinitionCacheDTO dto);

        IAICache AI { get; }
        IAppearanceCache Appearance { get; }
        ICollisionCache Collision { get; }
        ICharacteristicCache Characteristic { get; }
        IInventoryCache Inventory { get; }
        ILifetimeCache Lifetime { get; }
        IProjectileCache Projectile { get; }
        ITriggeredEffectCache TriggeredEffect { get; }
        IEntityCache Entity { get; }

        ILocaleCache Locale { get; }

        IEffectCache Effect { get; }
        IItemCache Item { get; }

        IRoomCache Room { get; }
    }
}
