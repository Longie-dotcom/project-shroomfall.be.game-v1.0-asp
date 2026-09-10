using Application.Service.EntityService;
using Application.Service.MetaService;
using Application.Service.WorldService;
using Application.Service.WorldService.Creation;
using Application.Service.WorldService.Factory;
using Application.Service.WorldService.Factory.Component;
using Application.Service.WorldService.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configuration
{
    public static class ServiceConfiguration
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        #region Methods
        public static IServiceCollection AddServiceConfiguration(
            this IServiceCollection services)
        {
            // ENTITY SERVICE
            services.AddSingleton<AIService>();
            services.AddSingleton<AppearanceService>();
            services.AddSingleton<CollisionService>();
            services.AddSingleton<CharacteristicService>();
            services.AddSingleton<InventoryService>();
            services.AddSingleton<LifetimeService>();
            services.AddSingleton<ProjectileService>();
            services.AddSingleton<TransformService>();
            services.AddSingleton<TriggeredEffectService>();

            // META SERVICE
            services.AddSingleton<DeathService>();
            services.AddSingleton<EffectService>();
            services.AddSingleton<ItemService>();
            services.AddSingleton<VitalService>();

            // WORLD SERVICE
            services.AddSingleton<EntitySpawnService>();
            services.AddSingleton<InitializationService>();

            services.AddSingleton<DefinitionRuntimeFactory>();
            services.AddSingleton<SnapshotRuntimeFactory>();
            services.AddSingleton<EntityInstanceFactory>();
            services.AddSingleton<RoomSpatialFactory>();

            services.AddScoped<EntityPersistence>();
            services.AddScoped<RoomPersistence>();
            services.AddScoped<SnapshotPersistence>();

            services.AddSingleton<BootstrapService>();
            services.AddSingleton<ResidencyService>();
            services.AddSingleton<RoomMigrationService>();
            services.AddSingleton<WorldContext>();

            return services;
        }
        #endregion
    }
}