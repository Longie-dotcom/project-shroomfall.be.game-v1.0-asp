using Application.Feature;
using Application.Feature.Abstraction;
using Application.Feature.Admin.Command;
using Application.Feature.Admin.Handler;
using Application.Feature.Connection.Command;
using Application.Feature.Connection.Handler;
using Application.Feature.Game.Command;
using Application.Feature.Game.Handler;
using Contract.DTO.Feature.Connection.Response;
using Contract.DTO.Feature.Design.Response;
using Contract.DTO.Runtime.WorldDomain;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configuration
{
    public static class FeatureConfiguration
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        #region Methods
        public static IServiceCollection AddFeatureConfiguration(
            this IServiceCollection services)
        {
            // CORE FEATURE
            services.AddScoped<IDispatcher, Dispatcher>();

            // ADMIN FEATURE
            services.AddScoped<IHandler<FetchRoomInstanceCommand, RoomInstanceDTO>, FetchRoomInstanceHandler>();
            services.AddScoped<IHandler<FetchRoomSpatialsCommand, List<RoomSpatialDTO>>, FetchRoomSpatialsHandler>();

            // CONNECTION FEATURE
            services.AddScoped<IHandler<CreateSessionCommand>, CreateSessionHandler>();
            services.AddScoped<IHandler<FetchSessionCommand, ExistedSessionDTO>, FetchSessionHandler>();
            services.AddScoped<IHandler<LoadSessionCommand, SaveGameDTO>, LoadSessionHandler>();
            services.AddScoped<IHandler<UnloadSessionCommand>, UnloadSessionHandler>();
            services.AddScoped<IHandler<UserConnectCommand>, UserConnectHandler>();
            services.AddScoped<IHandler<UserDisconnectCommand>, UserDisconnectHandler>();

            // GAME FEATURE
            services.AddScoped<IHandler<BackHomeCommand, SaveGameDTO>, BackHomeHandler>();
            services.AddScoped<IHandler<EnterHubCommand, SaveGameDTO>, EnterHubHandler>();
            services.AddScoped<IHandler<MoveCommand>, MoveHandler>();
            services.AddScoped<IHandler<UpdateAppearanceCommand>, UpdateAppearanceHandler>();
            services.AddScoped<IHandler<UseItemCommand>, UseItemHandler>();
            services.AddScoped<IHandler<UserRefreshCommand, DefinitionSnapshotDTO?>, UserRefreshHandler>();

            return services;
        }
        #endregion
    }
}