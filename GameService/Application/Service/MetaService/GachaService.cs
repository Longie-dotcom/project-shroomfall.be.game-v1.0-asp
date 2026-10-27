using Application.Interface.Cache;
using Application.Service.WorldService.Creation;
using Contract.DTO.Runtime.MetaDomain;
using Contract.Enum.MetaDomain.Item;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;

namespace Application.Service.MetaService
{
    public class GachaService
    {
        #region Attributes
        private readonly ICacheProvider cacheProvider;
        private readonly EntitySpawnService entitySpawnService;
        #endregion

        #region Properties
        #endregion

        public GachaService(
            ICacheProvider cacheProvider,
            EntitySpawnService entitySpawnService)
        {
            this.cacheProvider = cacheProvider;
            this.entitySpawnService = entitySpawnService;
        }

        #region Methods
        public bool TryGrantGachaReward(
            EntityInstance player,
            QuestRewardConfig config)
        {
            if (config == null || config.Entries == null || config.Entries.Count == 0)
                return false;

            var transform = player.GetComponent<TransformInstance>();
            if (transform == null)
                return false;

            // Validate reward pool.
            if (config.Entries.Any(x => string.IsNullOrWhiteSpace(x.ItemDefinitionId) 
            || x.Amount <= 0 || x.Weight < 0 || float.IsNaN(x.Weight) || float.IsInfinity(x.Weight)))
                return false;

            var totalWeight = config.Entries.Sum(x => (double)x.Weight);
            if (totalWeight <= 0)
                return false;

            // Select one reward using weighted randomness.
            var roll = Random.Shared.NextDouble() * totalWeight;
            double cumulativeWeight = 0;
            GachaRewardEntryDTO? selectedReward = null;

            foreach (var entry in config.Entries)
            {
                cumulativeWeight += entry.Weight;
                if (roll < cumulativeWeight)
                {
                    selectedReward = entry;
                    break;
                }
            }

            if (selectedReward == null)
                return false;

            // Retrieve the item definition from the cache.
            var itemDefinition = cacheProvider.Item.Get(selectedReward.ItemDefinitionId);
            if (itemDefinition == null)
                return false;

            // Randomly select an item quality.
            var qualities = Enum.GetValues<ItemQuality>();
            if (qualities.Length == 0)
                return false;

            var quality = qualities[Random.Shared.Next(qualities.Length)];

            // Initialize durability from the item definition.
            var item = new ItemInstance(
                Guid.NewGuid().ToString(),
                itemDefinition.Id,
                selectedReward.Amount,
                quality,
                itemDefinition.MaxDurability);

            // Spawn the item at the player's current location.
            entitySpawnService.Spawn(
                new WorldItemCreateContext(
                    Guid.NewGuid().ToString(),
                    transform.RoomSpatialID,
                    transform.LayerZ,
                    transform.Position,
                    item));

            return true;
        }
        #endregion
    }
}