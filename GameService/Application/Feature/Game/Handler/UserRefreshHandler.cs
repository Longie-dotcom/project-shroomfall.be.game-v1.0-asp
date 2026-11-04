using Application.Feature.Abstraction;
using Application.Feature.Game.Command;
using Application.Interface.Cache;
using AutoMapper;
using Contract.DTO.Definition.EntityDomain.Component;
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

        public DefinitionSnapshotDTO BuildDefinitionSnapshot(long version)
        {
            var entities = cacheProvider.Entity.GetAll().ToList();
            var characteristics = cacheProvider.Characteristic.GetAll().ToList();

            Console.WriteLine("========== CHARACTERISTIC DEBUG ==========");
            Console.WriteLine($"Total entities: {entities.Count}");
            Console.WriteLine($"Total characteristics: {characteristics.Count}");

            // 1. Inspect characteristic cache.
            foreach (var characteristic in characteristics)
            {
                Console.WriteLine(
                    $"[CACHE] ID: {characteristic.ID}, " +
                    $"Entity: {characteristic.EntityDefinitionID}");

                Console.WriteLine(
                    $"[CACHE] AttributeValues: {characteristic.AttributeValues?.Count ?? 0}");

                if (characteristic.AttributeValues != null)
                {
                    foreach (var attribute in characteristic.AttributeValues)
                    {
                        Console.WriteLine(
                            $"    Type: {attribute.Type}, " +
                            $"Value: {attribute.Value}, " +
                            $"Max: {attribute.Max}");
                    }
                }
            }

            // 2. Attach characteristics to their corresponding entities.
            foreach (var entity in entities)
            {
                Console.WriteLine($"[ENTITY] Attaching components to: {entity.Id}");

                var characteristic = characteristics.FirstOrDefault(
                    x => x.EntityDefinitionID == entity.Id);

                if (characteristic == null)
                {
                    Console.WriteLine($"[WARNING] No characteristic found for {entity.Id}");
                    continue;
                }

                Console.WriteLine(
                    $"[ATTACH] {entity.Id} -> {characteristic.AttributeValues?.Count ?? 0} attributes");

                entity.Components.Add(characteristic);
            }

            // 3. Inspect final entity components.
            foreach (var entity in entities)
            {
                foreach (var characteristic in entity.Components
                             .OfType<CharacteristicDefinitionDTO>())
                {
                    Console.WriteLine(
                        $"[FINAL] Entity: {entity.Id}, " +
                        $"Characteristic attributes: {characteristic.AttributeValues?.Count ?? 0}");
                }
            }

            Console.WriteLine("========== END CHARACTERISTIC DEBUG ==========");

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