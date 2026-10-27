using Application.Interface.Cache;
using Contract;
using Contract.Common;
using Domain.DomainException;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using Domain.Shared;
using ResponseCode;

namespace Application.Service.WorldService.Factory.Component
{
    public class DefinitionRuntimeFactory
    {
        #region Attributes
        #endregion

        #region Properties
        private readonly ICacheProvider cacheProvider;
        #endregion

        public DefinitionRuntimeFactory(
            ICacheProvider cacheProvider) 
        {
            this.cacheProvider = cacheProvider;
        }

        #region Methods
        public AIInstance CreateAI(
            string entityDefinitionId)
        {
            var aiDef = cacheProvider.AI.GetByEntity(entityDefinitionId);
            if (aiDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.AIDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing AI definition.");

            return new AIInstance(
                aiDef.ID!.Value,
                aiDef.LeashDistance,
                aiDef.AggroRadius,
                aiDef.IsAIControlled,
                aiDef.ThinkInterval,
                aiDef.EquippedItemDefinitionID,
                aiDef.AttackRange);
        }

        public AppearanceInstance CreateAppearance(
            string entityDefinitionId)
        {
            var appearanceDef = cacheProvider.Appearance.GetByEntity(entityDefinitionId);
            if (appearanceDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.AppearanceDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing Appearance definition.");

            return new AppearanceInstance(
                appearanceDef.ID!.Value,
                appearanceDef.SkinID ?? "",
                appearanceDef.SkinColor);
        }

        public CollisionInstance CreateCollision(
            string entityDefinitionId)
        {
            var collisionDef = cacheProvider.Collision.GetByEntity(entityDefinitionId);
            if (collisionDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.CollisionDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing Collision definition.");

            return new CollisionInstance(
                collisionDef.ID!.Value,
                CollisionShapeMapper.FromDefinition(collisionDef),
                new Vector2(collisionDef.OffsetX, collisionDef.OffsetY),
                collisionDef.Layer,
                collisionDef.Mask);
        }

        public CharacteristicInstance CreateCharacteristic(
            string entityDefinitionId)
        {
            var characteristicDef = cacheProvider.Characteristic.GetByEntity(entityDefinitionId);
            if (characteristicDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.CharacteristicDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing Characteristic definition.");

            return new CharacteristicInstance(
                characteristicDef.ID!.Value,
                Constraint.DEFAULT_CHARACTERISTIC_LEVEL);
        }

        public InventoryInstance CreateInventory(
            string entityDefinitionId)
        {
            var inventoryDef = cacheProvider.Inventory.GetByEntity(entityDefinitionId);
            if (inventoryDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.InventoryDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing Inventory definition.");

            var validDefaultItems = new List<ItemInstance>();
            foreach (var entry in inventoryDef.DefaultItems)
            {
                var itemDef = cacheProvider.Item.Get(entry.DefinitionID);
                if (itemDef == null)
                    continue;

                int remaining = entry.Amount;
                int maxStack = itemDef.MaxStack ?? 1;

                while (remaining > 0)
                {
                    int amount = Math.Min(remaining, maxStack);

                    validDefaultItems.Add(new ItemInstance(
                        Guid.NewGuid().ToString(),
                        itemDef.Id,
                        amount,
                        entry.Quality,
                        itemDef.MaxDurability));

                    remaining -= amount;
                }
            }

            return new InventoryInstance(
                inventoryDef.ID!.Value,
                validDefaultItems);
        }

        public LifetimeInstance CreateLifeTime(
            string entityDefinitionId)
        {
            var lifetimeDef = cacheProvider.Lifetime.GetByEntity(entityDefinitionId);
            if (lifetimeDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.LifetimeDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing Lifetime definition.");

            return new LifetimeInstance(
                lifetimeDef.ID!.Value,
                lifetimeDef.Duration);
        }

        public ProjectileInstance CreateProjectile(
            string entityDefinitionId)
        {
            var projectileDef = cacheProvider.Projectile.GetByEntity(entityDefinitionId);
            if (projectileDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.ProjectileDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing Projectile configuration.");

            return new ProjectileInstance(
                projectileDef.ID!.Value,
                projectileDef.Velocity,
                projectileDef.OnImpactSpawnEntityDefinitionID);
        }

        public TriggeredEffectInstance CreateTriggeredEffect(
            string entityDefinitionId,
            string sourceEntityId)
        {
            var triggeredEffectDef = cacheProvider.TriggeredEffect.GetByEntity(entityDefinitionId);
            if (triggeredEffectDef == null)
                throw new InternalException(
                    ApplicationCode.DefinitionRuntimeFactoryCode.TriggeredEffectDefinitionNotFound,
                    $"Entity '{entityDefinitionId}' missing Triggered Effect configuration.");

            var validEffects = new List<string>();

            foreach (var entry in triggeredEffectDef.EffectDefinitionIDs)
            {
                var effectDef = cacheProvider.Effect.Get(entry);
                if (effectDef != null)
                    validEffects.Add(entry);
            }

            return new TriggeredEffectInstance(
                triggeredEffectDef.ID!.Value,
                validEffects,
                sourceEntityId);
        }

        public ActionInstance CreateAction()
        {
            return new ActionInstance();
        }

        public EconomicInstance CreateEconomic()
        {
            return new EconomicInstance(Constraint.DEFAULT_ECONOMY);
        }

        public EffectContainerInstance CreateEffectContainer()
        {
            return new EffectContainerInstance();
        }

        public OwnershipInstance CreateOwnership(
            string userId,
            string personalRoomId)
        {
            return new OwnershipInstance(userId, personalRoomId);
        }

        public QuestProgressInstance CreateQuestProgress()
        {
            return new QuestProgressInstance(new List<QuestProgressEntry>());
        }

        public TransformInstance CreateTransform(
            string roomSpatialId,
            int layerZ,
            Vector2 position)
        {
            return new TransformInstance(roomSpatialId, layerZ, position);
        }

        public WorldItemPayloadInstance CreateWorldItemPayload(
            ItemInstance itemInstance)
        {
            return new WorldItemPayloadInstance(itemInstance);
        }
        #endregion
    }
}