using Application.Interface.Realtime;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Managers;
using Infrastructure.Realtime;
using Infrastructure.Realtime.Events;
using Infrastructure.Realtime.Events.Admin;
using Infrastructure.Realtime.Events.Design;
using Infrastructure.Realtime.Events.Game;
using Infrastructure.Realtime.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Configuration
{
    public static class RealtimeConfiguration
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        #region Methods
        public static IServiceCollection AddRealtimeConfiguration(
            this IServiceCollection services)
        {
            // CORE REALTIME
            services.AddSignalR();
            services.AddSingleton<IRealtimePublisher, RealtimePublisher>();

            // ADMIN HANDLER
            services.AddSingleton<IEventHandler, RoomStateChangedHandler>();
            services.AddSingleton<IEventHandler, RoomSyncChangedHandler>();
            services.AddSingleton<IEventHandler, UserConnectionChangedHandler>();
            services.AddSingleton<IEventHandler, UserSessionChangedHandler>();

            // DESIGN HANDLER
            services.AddSingleton<IEventHandler, DefinitionUpdatedHandler>();

            // GAME HANDLER
            services.AddSingleton<IEventHandler, EntityActedHandler>();
            services.AddSingleton<IEventHandler, EntityAppearanceChangedHandler>();
            services.AddSingleton<IEventHandler, EntityLifecycleHandler>();
            services.AddSingleton<IEventHandler, EntityVitalChangedHandler>();
            services.AddSingleton<IEventHandler, InventoryClearedHandler>();
            services.AddSingleton<IEventHandler, InventoryItemChangedHandler>();
            services.AddSingleton<IEventHandler, PlayerCharacteristicSyncHandler>();

            // EVENT BUS
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<IEventDispatcher, EventDispatcher>();

            // MANAGER
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<IConnectionManager, ConnectionManager>();

            return services;
        }
        #endregion
    }
}