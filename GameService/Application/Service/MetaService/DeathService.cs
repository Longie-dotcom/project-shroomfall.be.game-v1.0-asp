using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Service.EntityService;
using Application.Service.WorldService.Creation;
using Contract;
using Contract.Enum.MetaDomain.Effect;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;

namespace Application.Service.MetaService
{
    public enum DeathOutcome
    {
        None,
        Entity,
        Player
    }

    public class DeathService
    {
        #region Attributes
        private readonly IEventBus eventBus;
        private readonly InventoryService inventoryService;
        private readonly VitalService vitalService;
        private readonly EntitySpawnService entitySpawnService;
        #endregion

        #region Properties
        #endregion

        public DeathService(
            IEventBus eventBus,
            InventoryService inventoryService,
            VitalService vitalService,
            EntitySpawnService entitySpawnService)
        {
            this.eventBus = eventBus;
            this.inventoryService = inventoryService;
            this.vitalService = vitalService;
            this.entitySpawnService = entitySpawnService;
        }

        #region Methods
        public DeathOutcome CheckDeath(
            EntityInstance entity,
            AttributeType vital,
            float previousValue,
            float currentValue)
        {
            if (vital != AttributeType.Health || currentValue > 0f)
                return DeathOutcome.None;

            var ownership = entity.GetComponent<OwnershipInstance>();

            // Case A: Unowned entity (e.g., wild monsters, creeps)
            if (ownership == null)
            {
                return DeathOutcome.Entity;
            }
            // Case B: Player in an active combat run
            else
            {
                return DeathOutcome.Player;
            }
        }

        /// <summary>
        /// Applies death penalties in-tick (money loss, random drops, health restore)
        /// and publishes <see cref="PlayerDiedEvent"/> so the respawn migration runs outside the tick.
        /// </summary>
        public void ApplyPlayerDeath(
            EntityInstance player)
        {
            // Several lethal hits in one tick enqueue several deaths; health is already restored after the first
            var characteristic = player.GetComponent<CharacteristicInstance>();
            if (characteristic != null && characteristic.GetVital(AttributeType.Health) > 0f)
                return;

            // Lose money
            var economic = player.GetComponent<EconomicInstance>();
            if (economic != null)
            {
                int lost = (int)MathF.Floor(economic.CurrentMoney * Constraint.PLAYER_DEATH_MONEY_LOSS_RATE);
                if (lost > 0 && economic.TrySpendMoney(lost))
                    eventBus.Publish(new EconomicUpdatedEvent(player.ID, economic.CurrentMoney));
            }

            // Drop random items where the player died
            var transform = player.GetComponent<TransformInstance>();
            if (transform != null && player.GetComponent<InventoryInstance>() != null)
            {
                var drops = inventoryService.DropRandomItems(player, Constraint.PLAYER_DEATH_DROP_ITEM_COUNT);
                foreach (var item in drops)
                {
                    entitySpawnService.Spawn(
                        new WorldItemCreateContext(
                            Guid.NewGuid().ToString(),
                            transform.RoomSpatialID,
                            transform.LayerZ,
                            transform.Position,
                            item));
                }
            }

            // Restore health
            vitalService.RestoreVitalByRate(player, AttributeType.Health, Constraint.PLAYER_RESPAWN_HEALTH_RATE);

            // Respawn at personal room (async, outside the tick)
            eventBus.Publish(new PlayerDiedEvent(player.ID));
        }
        #endregion
    }
}
