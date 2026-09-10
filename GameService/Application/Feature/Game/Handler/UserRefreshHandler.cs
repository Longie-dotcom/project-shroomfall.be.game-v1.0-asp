using Application.Feature.Abstraction;
using Application.Feature.Game.Command;
using Application.Interface.Cache;
using AutoMapper;
using Contract.DTO.Definition.EntityDomain.Component;
using Contract.DTO.Definition.LocalizationDomain;
using Contract.DTO.Definition.MetaDomain;
using Contract.DTO.Definition.WorldDomain;
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
            var allRooms = cacheProvider.Room.GetAll();
            var effects = mapper.Map<List<EffectDefinitionDTO>>(cacheProvider.Effect.GetAll());
            var items = mapper.Map<List<ItemDefinitionDTO>>(cacheProvider.Item.GetAll());
            var entities = mapper.Map<List<EntityDefinitionDTO>>(cacheProvider.Entity.GetAll());
            var rooms = mapper.Map<List<RoomDefinitionDTO>>(allRooms);
            var locales = mapper.Map<List<LocaleDTO>>(cacheProvider.Locale.GetAll());

            return new DefinitionSnapshotDTO
            {
                Version = version,
                Effects = effects,
                Items = items,
                Entities = entities,
                Rooms = rooms,
                Locales = locales
            };
        }
        #endregion
    }
}