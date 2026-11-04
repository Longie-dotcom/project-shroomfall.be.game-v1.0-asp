using Application.Feature.Abstraction;
using Application.Feature.Game.Command;
using Application.Interface.Cache;
using AutoMapper;
using Contract.DTO.Feature.Design.Response;

namespace Application.Feature.Game.Handler
{
    public class UserRefreshHandler : IHandler<UserRefreshCommand, DefinitionSnapshotDTO?>
    {
        #region Attributes
        //private readonly IRelationalUoW relationalUoW;
        private readonly IMapper mapper;
        private readonly ICacheProvider cacheProvider;
        #endregion

        #region Properties
        #endregion

        public UserRefreshHandler(
            //IRelationalUoW relationalUoW,
            IMapper mapper,
            ICacheProvider cacheProvider)
        {
            //this.relationalUoW = relationalUoW;
            this.mapper = mapper;
            this.cacheProvider = cacheProvider;
        }

        #region Methods
        public async Task<DefinitionSnapshotDTO?> Handle(
            UserRefreshCommand command)
        {
            // TODO: USING GRPC TO GET LATEST VERSION

            var dto = command.DTO;

            // Resolve repository
            //var definitionVersionLogRepo = relationalUoW.GetRepository<IDefinitionVersionLogRepository>();

            // Get latest global definition version
            //var latest = await definitionVersionLogRepo.GetLatest(Constraint.GLOBAL_DEFINITION_VERSION);

            // No definition yet
            //if (latest == null)
            //    return null;

            // Client already latest
            //if (dto.DefinitionVersion == latest.Version.ToString())
            //    return null;

            // Return full snapshot
            return BuildDefinitionSnapshot(1);
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