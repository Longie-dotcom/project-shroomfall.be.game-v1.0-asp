using Application.Interface.Cache;
using Application.Interface.Cache.EntityDomain;
using Application.Interface.Cache.EntityDomain.Component;
using Application.Interface.Cache.LocalizationDomain;
using Application.Interface.Cache.MetaDomain;
using Application.Interface.Cache.WorldDomain;
using Infrastructure.Cache;
using Infrastructure.Cache.EntityDomain;
using Infrastructure.Cache.EntityDomain.Component;
using Infrastructure.Cache.LocalizationDomain;
using Infrastructure.Cache.MetaDomain;
using Infrastructure.Cache.WorldDomain;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Configuration
{
    public static class CacheConfiguration
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        #region Methods
        public static IServiceCollection AddCacheConfiguration(
            this IServiceCollection services)
        {
            // CACHE PROVIDER
            services.AddScoped<ICacheProvider, CacheProvider>();

            // ENTITY CACHE
            services.AddSingleton<IAICache, AICache>();
            services.AddSingleton<IAppearanceCache, AppearanceCache>();
            services.AddSingleton<ICollisionCache, CollisionCache>();
            services.AddSingleton<ICharacteristicCache, CharacteristicCache>();
            services.AddSingleton<IInventoryCache, InventoryCache>();
            services.AddSingleton<ILifetimeCache, LifetimeCache>();
            services.AddSingleton<IProjectileCache, ProjectileCache>();
            services.AddSingleton<ITriggeredEffectCache, TriggeredEffectCache>();
            services.AddSingleton<IEntityCache, EntityCache>();

            // LOCALIZATION CACHE
            services.AddSingleton<ILocaleCache, LocaleCache>();

            // META CACHE
            services.AddSingleton<IEffectCache, EffectCache>();
            services.AddSingleton<IItemCache, ItemCache>();

            // WORLD CACHE
            services.AddSingleton<IRoomCache, RoomCache>();

            return services;
        }
        #endregion
    }
}