using Application.Interface.Cache;
using Application.Service.EntityService;
using Application.Service.WorldService;
using Application.Service.WorldService.Creation;
using Application.System.Abstraction;
using Application.System.Queue;
using Contract;
using Contract.Common;
using Contract.DTO.Definition.MetaDomain;
using Contract.Enum.MetaDomain.Effect;
using Contract.Enum.MetaDomain.Item;
using Domain.DomainException;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using ResponseCode;

namespace Application.Service.MetaService
{
    public readonly struct ItemUsageActionContext
    {
        public ItemInstance Item { get; }
        public ItemDefinitionDTO ItemDef { get; }
        public ItemUsageAction Type { get; }
        public Vector2? TargetVector { get; }
        public EquipmentSlot? Slot { get; }

        public ItemUsageActionContext(
            ItemInstance item,
            ItemDefinitionDTO itemDef,
            ItemUsageAction type,
            Vector2? targetVector = null,
            EquipmentSlot? slot = null)
        {
            Item = item;
            ItemDef = itemDef;
            Type = type;
            TargetVector = targetVector;
            Slot = slot;
        }
    }

    public class ItemService : ITickService
    {
        #region Attributes
        private readonly WorldContext worldContext;
        private readonly ICacheProvider cacheProvider;
        private readonly EntitySpawnService entitySpawnService;
        private readonly EffectService effectService;
        private readonly InventoryService inventoryService;
        private readonly CharacteristicService characteristicService;
        #endregion

        #region Properties
        #endregion

        public ItemService(
            WorldContext worldContext,
            ICacheProvider cacheProvider,
            EntitySpawnService entitySpawnService,
            EffectService effectService,
            InventoryService inventoryService,
            CharacteristicService characteristicService)
        {
            this.worldContext = worldContext;
            this.cacheProvider = cacheProvider;
            this.entitySpawnService = entitySpawnService;
            this.effectService = effectService;
            this.inventoryService = inventoryService;
            this.characteristicService = characteristicService;
        }

        #region Methods
        public void Tick(
            float dt,
            CommandBuffer commandBuffer)
        {
            var entities = worldContext.GetEntities().ToList();

            foreach (var entity in entities)
            {
                var characteristic = entity.GetComponent<CharacteristicInstance>();
                if (characteristic == null)
                    continue;

                var actionState = entity.GetComponent<ActionInstance>();
                if (actionState == null)
                    continue;

                if (!actionState.CanUseItems)
                {
                    actionState.ClearItemUseIntent();
                    continue;
                }

                // 1. Update active cooldown timers on the instance
                if (actionState.ActiveCooldowns.Count > 0)
                {
                    var keys = actionState.ActiveCooldowns.Keys.ToList();
                    foreach (var key in keys)
                    {
                        actionState.ActiveCooldowns[key] -= dt;
                        if (actionState.ActiveCooldowns[key] <= 0f)
                        {
                            actionState.ActiveCooldowns.Remove(key);
                        }
                    }
                }

                // 2. Process pending item uses
                if (actionState.PendingItemUseID != null)
                {
                    var inventory = entity.GetComponent<InventoryInstance>();
                    var item = inventory?.Items.FirstOrDefault(i => i.ID == actionState.PendingItemUseID);

                    // If the item isn't in their inventory, clear the intent and jump to the next entity
                    if (item == null)
                    {
                        actionState.ClearItemUseIntent();
                        continue; // CRITICAL: Use 'continue' instead of 'return'
                    }

                    // 3. Cooldown Gatekeeper Check
                    if (actionState.IsOnCooldown(item.ID))
                    {
                        actionState.ClearItemUseIntent();
                        continue;
                    }

                    var itemDef = cacheProvider.Item.Get(item.DefinitionID);
                    if (itemDef == null)
                    {
                        actionState.ClearItemUseIntent();
                        continue; // CRITICAL: Use 'continue' instead of 'return'
                    }

                    // Create the correct usage action object based on the enum intent and available data
                    ItemUsageActionContext context = new ItemUsageActionContext(
                        item,
                        itemDef,
                        actionState.ItemUsageAction,
                        actionState.PendingTargetPosition,
                        actionState.PendingUnequippedSlot);

                    // Enqueue the command with the typed action object
                    commandBuffer.Commands.Enqueue(new ItemActionCommand(
                        entity.ID,
                        context));

                    var cdr = characteristic.GetCore(AttributeType.CooldownReduction); // e.g., 0.20 for 20% reduction

                    // Calculate actual duration: e.g., Cooldown * (1 - 0.20)
                    float modifiedCooldown = Constraint.ITEM_COOLDOWN_VALUE * (1f - cdr);

                    actionState.ApplyCooldown(item.ID, MathF.Max(0f, modifiedCooldown));

                    // Clear the intent
                    actionState.ClearItemUseIntent();
                }
            }
        }
        #endregion

        #region Core Pipeline
        /// <summary>
        /// Orchestrates the decoupled Manifestation and Cost logic.
        /// </summary>
        public void Execute(
            EntityInstance entity,
            ItemUsageActionContext action)
        {
            // Material is for crafting and trading only!
            if (action.ItemDef.Category == ItemCategory.Material)
                return;

            // Deduct energy
            characteristicService.ConsumeItemUsageEnergy(entity, Constraint.ITEM_ENERGY_USAGE_CONSUME);

            switch (action.Type)
            {
                case ItemUsageAction.Unequip:
                    ExecuteUnequip(
                        entity,
                        action.Slot!.Value);
                    break;

                case ItemUsageAction.Use:
                    ExecuteManifestation(
                        entity,
                        action.Item,
                        action.ItemDef,
                        action.TargetVector!);

                    DeductCost(
                        entity,
                        action.Item,
                        action.ItemDef);

                    break;
            }
        }
        #endregion

        #region Private Manifestation Steps (World Changes)
        private void ExecuteManifestation(
            EntityInstance entity,
            ItemInstance item,
            ItemDefinitionDTO itemDef,
            Vector2 targetVector)
        {
            switch (itemDef.Category)
            {
                case ItemCategory.Equippable:
                    if (itemDef.EquippableConfig != null)
                    {
                        ExecuteEquippable(entity, itemDef.EquippableConfig, item);
                    }
                    break;

                case ItemCategory.Placeable:
                    if (itemDef.PlaceableConfig != null)
                    {
                        ExecutePlaceable(entity, itemDef.PlaceableConfig, targetVector);
                    }
                    break;

                case ItemCategory.Ranged:
                    if (itemDef.RangedConfig != null)
                    {
                        ExecuteRanged(entity, itemDef.RangedConfig, targetVector);
                    }
                    break;

                case ItemCategory.Melee:
                    if (itemDef.MeleeConfig != null)
                    {
                        ExecuteMelee(entity, itemDef.MeleeConfig, targetVector);
                    }
                    break;

                case ItemCategory.Consumable:
                    if (itemDef.ConsumableConfig != null)
                    {
                        ExecuteConsumable(entity, itemDef.ConsumableConfig);
                    }
                    break;

                case ItemCategory.Material:
                    break;
            }
        }

        private void ExecuteEquippable(
            EntityInstance entity,
            EquippableConfigDTO config,
            ItemInstance item)
        {
            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory == null)
                return;

            var unequippedItem = inventoryService.EquipItem(entity, item, config.Slot);
            if (unequippedItem != null)
                RemoveItemEffects(entity, unequippedItem);

            foreach (var effectId in config.EffectDefinitionIDs)
            {
                var effectDef = cacheProvider.Effect.Get(effectId);
                if (effectDef == null)
                    continue;

                effectService.ApplyEffect(new EffectContext()
                {
                    Target = entity,
                    Source = null,
                    Effect = effectDef,
                });
            }
        }

        private void ExecuteUnequip(
            EntityInstance entity,
            EquipmentSlot slot)
        {
            var unequippedItem = inventoryService.UnequipItem(entity, slot);
            if (unequippedItem != null)
                RemoveItemEffects(entity, unequippedItem);
        }

        private void ExecutePlaceable(
            EntityInstance entity,
            PlaceableConfigDTO config,
            Vector2 targetVector)
        {
            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.ItemServiceCode.PlaceableMissingTransform,
                    $"Entity {entity.ID} missing TransformInstance when placing object.");

            var spawnContext = new WorldEntityCreateContext(
                Guid.NewGuid().ToString(),
                config.EntityDefinitionID,
                transform.RoomSpatialID,
                transform.LayerZ,
                targetVector
            );

            entitySpawnService.Spawn(spawnContext);
        }

        private void ExecuteRanged(
            EntityInstance entity,
            RangedConfigDTO config,
            Vector2 targetVector)
        {
            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.ItemServiceCode.RangedMissingTransform,
                    $"Entity {entity.ID} missing TransformInstance for using ranged.");

            var (projectileSpawnPos, finalDirection) = ResolveProjectileSpawn(entity, transform, targetVector);

            var spawnContext = new ProjectileEntityCreateContext(
                Guid.NewGuid().ToString(),
                config.EntityDefinitionID,
                transform.RoomSpatialID,
                transform.LayerZ,
                projectileSpawnPos,
                finalDirection,
                entity.ID
            );

            entitySpawnService.Spawn(spawnContext);
        }

        private void ExecuteMelee(
            EntityInstance entity,
            MeleeConfigDTO config,
            Vector2 targetVector)
        {
            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.ItemServiceCode.MeleeMissingTransform,
                    $"Entity {entity.ID} missing TransformInstance for using melee.");

            var (projectileSpawnPos, finalDirection) = ResolveProjectileSpawn(entity, transform, targetVector);

            var spawnContext = new ProjectileEntityCreateContext(
                Guid.NewGuid().ToString(),
                config.EntityDefinitionID,
                transform.RoomSpatialID,
                transform.LayerZ,
                projectileSpawnPos,
                finalDirection,
                entity.ID
            );

            entitySpawnService.Spawn(spawnContext);
        }

        private void ExecuteConsumable(
            EntityInstance entity,
            ConsumableConfigDTO config)
        {
            foreach (var effectId in config.EffectDefinitionIDs)
            {
                var effectDef = cacheProvider.Effect.Get(effectId);
                if (effectDef == null)
                    continue;

                effectService.ApplyEffect(new EffectContext()
                {
                    Target = entity,
                    Source = null,
                    Effect = effectDef,
                });
            }
        }
        #endregion

        #region Private Cost Steps (Inventory Mutations)
        private void DeductCost(
            EntityInstance entity,
            ItemInstance item,
            ItemDefinitionDTO itemDef)
        {
            if (itemDef.CostConfig == null || itemDef.CostConfig.Method == ItemConsumptionMethod.None)
            {
                return;
            }

            switch (itemDef.CostConfig.Method)
            {
                case ItemConsumptionMethod.ConsumeStack:
                    inventoryService.DeductItem(entity, item);
                    break;

                case ItemConsumptionMethod.DegradeDurability:
                    inventoryService.DegradeItem(entity, item);
                    break;

                case ItemConsumptionMethod.RemoveEntirely:
                    inventoryService.RemoveItem(entity, item);
                    break;
            }
        }

        private (Vector2 startPos, Vector2 direction) ResolveProjectileSpawn(
            EntityInstance entity,
            TransformInstance transform,
            Vector2 targetVector)
        {
            var collision = entity.GetComponent<CollisionInstance>();

            // 1. Calculate caster's true collision center (incorporating the collision offset)
            Vector2 casterCenter = transform.Position;
            if (collision != null)
            {
                casterCenter += collision.CollisionOffset;
            }

            Vector2 targetPos = targetVector;

            // 2. Calculate and normalize the projectile direction
            Vector2 rawDir = targetPos - casterCenter;
            Vector2 finalDirection = Vector2.Normalize(rawDir);

            // 3. Dynamic Spawn Offset based on Caster's Collision Shape
            float spawnOffsetDist = 0.5f; // Fallback distance

            if (collision != null)
            {
                switch (collision.CollisionShape)
                {
                    case CircleShape circle:
                        // Spawn just outside the circle's radius
                        spawnOffsetDist = circle.Radius + 0.1f;
                        break;

                    case BoxShape box:
                        // Project direction vector onto the box edges to find the boundary distance
                        float halfW = box.Width / 2f;
                        float halfH = box.Height / 2f;

                        float absDirX = MathF.Abs(finalDirection.X);
                        float absDirY = MathF.Abs(finalDirection.Y);

                        // Find intersection of direction ray with AABB boundary
                        float safeDirX = MathF.Max(absDirX, 0.00001f);
                        float safeDirY = MathF.Max(absDirY, 0.00001f);

                        float distToXEdge = halfW / safeDirX;
                        float distToYEdge = halfH / safeDirY;

                        spawnOffsetDist = MathF.Min(distToXEdge, distToYEdge) + 0.1f; // Add a tiny 0.1 padding
                        break;

                    case PointShape:
                        spawnOffsetDist = 0.1f; // Points have no volume, spawn almost instantly at center
                        break;
                }
            }

            // 4. Calculate final Spawn Position
            Vector2 projectileSpawnPos = casterCenter + (finalDirection * spawnOffsetDist);

            return (projectileSpawnPos, finalDirection);
        }

        private void RemoveItemEffects(
            EntityInstance entity,
            ItemInstance item)
        {
            var itemDef = cacheProvider.Item.Get(item.DefinitionID);
            if (itemDef?.EquippableConfig?.EffectDefinitionIDs != null)
            {
                foreach (var effectId in itemDef.EquippableConfig.EffectDefinitionIDs)
                {
                    var effectDef = cacheProvider.Effect.Get(effectId);
                    if (effectDef == null)
                        continue;

                    effectService.RemoveEffect(new EffectContext()
                    {
                        Target = entity,
                        Source = null,
                        Effect = effectDef,
                    });
                }
            }
        }
        #endregion
    }
}