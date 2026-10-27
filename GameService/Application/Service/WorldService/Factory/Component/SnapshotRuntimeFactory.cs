using Application.Interface.Cache;
using Contract.Common;
using Domain.Abstraction;
using Domain.DomainException;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using Domain.Shared;
using Domain.Snapshot.EntityDomain.Component;
using ResponseCode;

namespace Application.Service.WorldService.Factory.Component
{
    public class SnapshotRuntimeFactory
    {
        #region Attributes
        private readonly ICacheProvider cacheProvider;
        #endregion

        public SnapshotRuntimeFactory(
            ICacheProvider cacheProvider)
        {
            this.cacheProvider = cacheProvider;
        }

        #region Methods
        public ComponentInstance Create(
            ComponentSnapshot snapshot)
        {
            return snapshot switch
            {
                ActionSnapshot action =>
                    CreateAction(action),
                AISnapshot ai => 
                    CreateAI(ai),
                AppearanceSnapshot appearance => 
                    CreateAppearance(appearance),
                CollisionSnapshot collision => 
                    CreateCollision(collision),
                CharacteristicSnapshot characteristic =>
                    CreateCharacteristic(characteristic),
                EconomicSnapshot economic =>
                    CreateEconomic(economic),
                EffectContainerSnapshot effectContainer => 
                    CreateEffectContainer(effectContainer),
                InventorySnapshot inventory => 
                    CreateInventory(inventory),
                LifetimeSnapshot lifetime => 
                    CreateLifetime(lifetime),
                OwnershipSnapshot ownership => 
                    CreateOwnership(ownership),
                ProjectileSnapshot projectile => 
                    CreateProjectile(projectile),
                QuestProgressSnapshot questProgress =>
                    CreateQuestProgress(questProgress),
                TransformSnapshot transform => 
                    CreateTransform(transform),
                TriggeredEffectSnapshot effectSnapshot =>
                    CreateTriggeredEffect(effectSnapshot),
                WorldItemPayloadSnapshot payload => 
                    CreateWorldItemPayload(payload),
                _ => throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.SnapshotTypeNotSupported,
                    $"Snapshot type {snapshot.GetType().Name} is not supported by factory.")
            };
        }

        private ActionInstance CreateAction(
            ActionSnapshot snapshot)
        {
            return new ActionInstance();
        }

        private AIInstance CreateAI(
            AISnapshot snapshot)
        {
            var def = cacheProvider.AI.GetByEntity(snapshot.EntityDefinitionID);
            if (def == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.AIDefinitionNotFound,
                    $"AI definition not found for entity: {snapshot.EntityDefinitionID}");

            return new AIInstance(
                def.ID!.Value,
                def.LeashDistance,
                def.AggroRadius,
                def.IsAIControlled,
                def.ThinkInterval,
                def.EquippedItemDefinitionID,
                def.AttackRange);
        }

        private AppearanceInstance CreateAppearance(
            AppearanceSnapshot snapshot)
        {
            return new AppearanceInstance(
                Guid.Parse(snapshot.DefinitionID),
                snapshot.SkinID,
                snapshot.SkinColor);
        }

        private CollisionInstance CreateCollision(
            CollisionSnapshot snapshot)
        {
            var def = cacheProvider.Collision.GetByEntity(snapshot.EntityDefinitionID);
            if (def == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.CollisionDefinitionNotFound,
                    $"Collision definition not found for entity: {snapshot.EntityDefinitionID}");

            return new CollisionInstance(
                def.ID!.Value,
                CollisionShapeMapper.FromDefinition(def),
                new Vector2(def.OffsetX, def.OffsetY),
                def.Layer,
                def.Mask);
        }

        private CharacteristicInstance CreateCharacteristic(
            CharacteristicSnapshot snapshot)
        {
            var def = cacheProvider.Characteristic.GetByEntity(snapshot.EntityDefinitionID);
            if (def == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.CharacteristicDefinitionNotFound,
                    $"Characteristic definition not found for entity: {snapshot.EntityDefinitionID}");

            var instance = new CharacteristicInstance(
                def.ID!.Value, 
                snapshot.CurrentLevel);

            // Restore current vitals
            foreach (var vital in snapshot.Vitals)
            {
                instance.SetVital(vital.Key, vital.Value);
            }

            return instance;
        }

        private EconomicInstance CreateEconomic(
            EconomicSnapshot snapshot)
        {
            return new EconomicInstance(snapshot.CurrentMoney);
        }

        private EffectContainerInstance CreateEffectContainer(
            EffectContainerSnapshot snapshot)
        {
            return new EffectContainerInstance();
        }
        
        private InventoryInstance CreateInventory(
            InventorySnapshot snapshot)
        {
            var def = cacheProvider.Inventory.GetByEntity(snapshot.EntityDefinitionID);
            if (def == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.InventoryDefinitionNotFound,
                    $"Inventory definition not found for entity: {snapshot.EntityDefinitionID}");

            // Restore item assets
            var validItemInstances = new List<ItemInstance>();
            foreach (var i in snapshot.Items)
            {
                var itemDef = cacheProvider.Item.Get(i.DefinitionID);
                if (itemDef != null)
                {
                    validItemInstances.Add(new ItemInstance(
                        i.ID,
                        i.DefinitionID,
                        i.Amount,
                        i.Quality,
                        i.Durability,
                        i.EquippedSlot));
                }
            }

            return new InventoryInstance(
                def.ID!.Value,
                validItemInstances);
        }

        private LifetimeInstance CreateLifetime(
            LifetimeSnapshot snapshot)
        {
            var def = cacheProvider.Lifetime.GetByEntity(snapshot.EntityDefinitionID);
            if (def == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.LifetimeDefinitionNotFound,
                    $"Lifetime definition not found for entity: {snapshot.EntityDefinitionID}");

            return new LifetimeInstance(
                def.ID!.Value,
                def.Duration,
                snapshot.ElapsedLifetime);
        }

        private OwnershipInstance CreateOwnership(
            OwnershipSnapshot snapshot)
        {
            return new OwnershipInstance(
                snapshot.UserID,
                snapshot.PersonalRoomID);
        }

        private ProjectileInstance CreateProjectile(
            ProjectileSnapshot snapshot)
        {
            var def = cacheProvider.Projectile.GetByEntity(snapshot.EntityDefinitionID);
            if (def == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.ProjectileDefinitionNotFound,
                    $"Projectile definition not found for entity: {snapshot.EntityDefinitionID}");

            return new ProjectileInstance(
                def.ID!.Value,
                def.Velocity,
                def.OnImpactSpawnEntityDefinitionID);
        }

        private QuestProgressInstance CreateQuestProgress(
            QuestProgressSnapshot snapshot)
        {
            var entries = snapshot.Entries
                .Select(entry => new QuestProgressEntry(
                    entry.QuestInstanceId,
                    entry.StartedAt,
                    entry.Status,
                    entry.IdentificationProgress is null
                        ? null
                        : new IdentificationProgress(
                            entry.IdentificationProgress.CorrectDefeatCount,
                            entry.IdentificationProgress.RequiredDefeatCount
                        )
                ))
                .ToList();

            return new QuestProgressInstance(entries);
        }

        private TransformInstance CreateTransform(
            TransformSnapshot snapshot)
        {
            return new TransformInstance(
                snapshot.RoomSpatialID,
                snapshot.LayerZ,
                snapshot.Position);
        }

        private TriggeredEffectInstance CreateTriggeredEffect(
            TriggeredEffectSnapshot snapshot)
        {
            var def = cacheProvider.TriggeredEffect.GetByEntity(snapshot.EntityDefinitionID);
            if (def == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.TriggeredEffectDefinitionNotFound,
                    $"Triggered Effect definition not found for entity: {snapshot.EntityDefinitionID}");

            var validEffects = new List<string>();
            foreach (var effectId in def.EffectDefinitionIDs)
            {
                if (cacheProvider.Effect.Get(effectId) != null)
                {
                    validEffects.Add(effectId);
                }
            }

            return new TriggeredEffectInstance(
                def.ID!.Value,
                validEffects,
                snapshot.SourceEntityID);
        }

        private WorldItemPayloadInstance CreateWorldItemPayload(
            WorldItemPayloadSnapshot snapshot)
        {
            var itemDef = cacheProvider.Item.Get(snapshot.Payload.DefinitionID);
            if (itemDef == null)
                throw new InternalException(
                    ApplicationCode.SnapshotRuntimeFactoryCode.ItemDefinitionNotFound,
                    $"Item definition not found for world item payload: {snapshot.Payload.DefinitionID}");

            var itemPayload = new ItemInstance(
                snapshot.Payload.ID,
                snapshot.Payload.DefinitionID,
                snapshot.Payload.Amount,
                snapshot.Payload.Quality,
                snapshot.Payload.Durability);

            return new WorldItemPayloadInstance(itemPayload);
        }
        #endregion
    }
}