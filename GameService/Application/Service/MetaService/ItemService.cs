using Application.Interface.Cache;
using Application.Service.EntityService;
using Application.Service.WorldService;
using Application.Service.WorldService.Creation;
using Application.System.Abstraction;
using Application.System.Queue;
using Contract;
using Contract.Common;
using Contract.DTO.Definition.MetaDomain;
using Contract.Enum.EntityDomain;
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

        #region Crafting
        public void Craft(
            EntityInstance entity,
            string itemDefinitionId)
        {
            var itemDef = cacheProvider.Item.Get(itemDefinitionId);
            if (itemDef == null)
                throw new NotFound(
                    ApplicationCode.ItemServiceCode.CraftItemDefinitionNotFound,
                    $"Item definition {itemDefinitionId} not found.");

            if (itemDef.RequiredMaterials.Count == 0)
                throw new BadRequest(
                    ApplicationCode.ItemServiceCode.CraftItemNotCraftable,
                    $"Item definition {itemDefinitionId} has no recipe.");

            var transform = entity.GetComponent<TransformInstance>();
            if (transform == null)
                throw new InternalException(
                    ApplicationCode.ItemServiceCode.CraftMissingTransform,
                    $"Entity {entity.ID} missing TransformInstance when crafting.");

            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory == null)
                throw new InternalException(
                    ApplicationCode.ItemServiceCode.CraftMissingInventory,
                    $"Entity {entity.ID} missing InventoryInstance when crafting.");

            // 1. Validate every material first so nothing is consumed on a failed craft (equipped items don't count)
            foreach (var material in itemDef.RequiredMaterials)
            {
                int owned = inventory.Items
                    .Where(i => i.DefinitionID == material.MaterialItemDefinitionID && !i.IsEquipped())
                    .Sum(i => i.Amount);

                if (owned < material.Quantity)
                    throw new BadRequest(
                        ApplicationCode.ItemServiceCode.CraftNotEnoughMaterials,
                        $"Entity {entity.ID} needs {material.Quantity} of {material.MaterialItemDefinitionID}, has {owned}.");
            }

            // 2. Consume materials, one unit at a time across stacks
            foreach (var material in itemDef.RequiredMaterials)
            {
                int remaining = material.Quantity;
                var stacks = inventory.Items
                    .Where(i => i.DefinitionID == material.MaterialItemDefinitionID && !i.IsEquipped())
                    .ToList();

                foreach (var stack in stacks)
                {
                    while (remaining > 0 && stack.Amount > 0)
                    {
                        inventoryService.DeductItem(entity, stack);
                        remaining--;
                    }

                    if (remaining == 0)
                        break;
                }
            }

            // 3. Drop the crafted item at the entity's position
            var crafted = new ItemInstance(
                Guid.NewGuid().ToString(),
                itemDef.Id,
                1,
                ItemQuality.Low,
                itemDef.MaxDurability);

            entitySpawnService.Spawn(
                new WorldItemCreateContext(
                    Guid.NewGuid().ToString(),
                    transform.RoomSpatialID,
                    transform.LayerZ,
                    transform.Position,
                    crafted));
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
            if (action.ItemDef.Category == ItemCategory.Material)
                return;

            switch (action.Type)
            {
                case ItemUsageAction.Unequip:
                    ExecuteUnequip(entity, action.Slot!.Value);
                    break;

                case ItemUsageAction.Use:
                    bool requiresEnergy = RequiresEnergy(action.ItemDef.Category);
                    if (requiresEnergy && !HasEnoughEnergy(entity))
                        return;

                    ExecuteManifestation(entity, action.Item, action.ItemDef, action.TargetVector!);

                    if (requiresEnergy)
                        characteristicService.ConsumeItemUsageEnergy(entity, Constraint.ITEM_ENERGY_USAGE_CONSUME);

                    DeductCost(entity, action.Item, action.ItemDef);
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
            //    (target on the caster's center would normalize to NaN, so fall back to the facing direction)
            Vector2 rawDir = targetPos - casterCenter;
            Vector2 finalDirection = rawDir.LengthSquared() > 0.0001f
                ? Vector2.Normalize(rawDir)
                : FacingToVector(transform.FacingDirection);

            // 3. Spawn on the circle that encloses the caster's collision, so the start distance is the same in every direction
            float spawnRadius = collision?.CollisionShape switch
            {
                CircleShape circle => circle.Radius,
                BoxShape box => MathF.Sqrt(box.Width * box.Width + box.Height * box.Height) / 2f, // half diagonal
                PointShape => 0f,
                _ => 0.4f // no collision: fallback body radius
            };

            float spawnOffsetDist = spawnRadius + 0.4f; // tiny padding so the projectile never starts inside the caster

            // 4. Calculate final Spawn Position
            Vector2 projectileSpawnPos = casterCenter + (finalDirection * spawnOffsetDist);

            return (projectileSpawnPos, finalDirection);
        }

        private static Vector2 FacingToVector(
            EntityDirection direction)
        {
            return direction switch
            {
                EntityDirection.UP => new Vector2(0, 1),
                EntityDirection.LEFT => new Vector2(-1, 0),
                EntityDirection.RIGHT => new Vector2(1, 0),
                _ => new Vector2(0, -1)
            };
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

        private bool RequiresEnergy(
            ItemCategory category)
        {
            return category == ItemCategory.Melee || category == ItemCategory.Ranged;
        }

        private bool HasEnoughEnergy(
            EntityInstance entity)
        {
            var characteristic = entity.GetComponent<CharacteristicInstance>();
            if (characteristic == null)
                return false;

            return characteristic.GetVital(AttributeType.Energy) >= Constraint.ITEM_ENERGY_USAGE_CONSUME;
        }
        #endregion
    }
}