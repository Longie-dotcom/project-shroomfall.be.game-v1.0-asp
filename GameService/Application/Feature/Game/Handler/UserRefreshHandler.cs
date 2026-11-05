using Application.Feature.Abstraction;
using Application.Feature.Game.Command;
using Application.Interface.Cache;
using Application.Interface.GrpcClient;
using AutoMapper;
using Contract.DTO.Feature.Design.Response;

namespace Application.Feature.Game.Handler
{
    public class UserRefreshHandler : IHandler<UserRefreshCommand, DefinitionSnapshotDTO?>
    {
        #region Attributes
        private readonly IVersionGrpcClient versionGrpcClient;
        private readonly IMapper mapper;
        private readonly ICacheProvider cacheProvider;
        #endregion

        #region Properties
        #endregion

        public UserRefreshHandler(
            IVersionGrpcClient versionGrpcClient,
            IMapper mapper,
            ICacheProvider cacheProvider)
        {
            this.versionGrpcClient = versionGrpcClient;
            this.mapper = mapper;
            this.cacheProvider = cacheProvider;
        }

        #region Methods
        public async Task<DefinitionSnapshotDTO?> Handle(
            UserRefreshCommand command)
        {
            var dto = command.DTO;

            // Get latest global definition version
            var current = await versionGrpcClient.RequestVersioningAsync();

            // No definition yet
            if (current == 0)
                return null;

            // Client already latest
            if (long.TryParse(dto.DefinitionVersion, out long clientVersion) && clientVersion == current)
                return null;

            // Return full snapshot
            return BuildDefinitionSnapshot(current);
        }

        public DefinitionSnapshotDTO BuildDefinitionSnapshot(
            long version)
        {
            var entities = cacheProvider.Entity.GetAll().ToList();
            var characteristics = cacheProvider.Characteristic.GetAll();

            // Attach characteristics to their corresponding entities.
            foreach (var entity in entities)
            {
                var characteristic = characteristics
                    .FirstOrDefault(x => x.EntityDefinitionID == entity.Id);

                if (characteristic != null)
                    entity.Components.Add(characteristic);
            }

            return new DefinitionSnapshotDTO
            {
                Version = version,
                Effects = cacheProvider.Effect.GetAll().ToList(),
                Items = cacheProvider.Item.GetAll().ToList(),
                Entities = entities,
                Rooms = cacheProvider.Room.GetAll().ToList(),
                Locales = cacheProvider.Locale.GetAll().ToList()
            };
        }
        #endregion
    }
}