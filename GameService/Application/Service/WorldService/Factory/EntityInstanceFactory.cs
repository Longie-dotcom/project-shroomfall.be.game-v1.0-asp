using Application.Interface.Cache;
using Application.Service.EntityService;
using Application.Service.MetaService;
using Application.Service.WorldService.Creation;
using Application.Service.WorldService.Factory.Component;
using Contract.Enum.EntityDomain;
using Domain.DomainException;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using Domain.Snapshot.EntityDomain;
using ResponseCode;

namespace Application.Service.WorldService.Factory
{
    public class EntityInstanceFactory
    {
        #region Attributes
        private readonly ICacheProvider cacheProvider;
        private readonly DefinitionRuntimeFactory definitionRuntimeFactory;
        private readonly SnapshotRuntimeFactory snapshotRuntimeFactory;
        private readonly CharacteristicService characteristicService;
        private readonly EffectService effectService;
        #endregion

        #region Properties
        #endregion

        public EntityInstanceFactory(
            ICacheProvider cacheProvider,
            DefinitionRuntimeFactory definitionRuntimeFactory,
            SnapshotRuntimeFactory snapshotRuntimeFactory,
            CharacteristicService characteristicService,
            EffectService effectService)
        {
            this.cacheProvider = cacheProvider;
            this.definitionRuntimeFactory = definitionRuntimeFactory;
            this.snapshotRuntimeFactory = snapshotRuntimeFactory;
            this.characteristicService = characteristicService;
            this.effectService = effectService;
        }

        #region Methods
        public EntityInstance Rehydrate(
            EntitySnapshot snapshot)
        {
            // Rehydrate entity instance and component instances
            var entity = new EntityInstance(snapshot.ID, snapshot.DefinitionID);
            foreach (var componentSnapshot in snapshot.Components)
            {
                var runtimeComponent = snapshotRuntimeFactory.Create(componentSnapshot);
                entity.AddComponent(runtimeComponent);
            }

            // Refresh entity equipment effects
            var inventory = entity.GetComponent<InventoryInstance>();
            if (inventory != null)
            {
                foreach (var equippedItem in inventory.GetAllEquipped().Values)
                {
                    var itemDef = cacheProvider.Item.Get(equippedItem.DefinitionID);
                    if (itemDef == null)
                        continue;

                    var config = itemDef.EquippableConfig;
                    if (config == null)
                        continue;

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
            }

            // Refresh entity characteristic
            characteristicService.InitializeCores(entity);

            return entity;
        }

        public EntityInstance Create(
            WorldEntityCreateContext context)
        {
            var entityDef = cacheProvider.Entity.Get(context.DefinitionID);
            if (entityDef == null)
                throw new InternalException(
                    ApplicationCode.EntityInstanceFactoryCode.EntityDefinitionNotFound,
                    $"Failed to create entity. Definition ID '{context.DefinitionID}' not found in cache.");

            var entity = new EntityInstance(context.InstanceID, entityDef.Id);
            ConstructEntity(entity, context);

            switch (entityDef.Type)
            {
                case EntityType.Projectile:
                    ConstructProjectile(entity, context);
                    break;
                case EntityType.WorldObject:
                    ConstructWorldObject(entity, context);
                    break;
                case EntityType.Creature:
                    ConstructCreature(entity, context);
                    break;
                case EntityType.Player:
                    ConstructPlayer(entity, context);
                    break;
                case EntityType.Item:
                    ConstructItem(entity, context);
                    break;
                default:
                    throw new InternalException(
                        ApplicationCode.EntityInstanceFactoryCode.EntityTypeNotSupported,
                        $"Entity creation failed. Entity type '{entityDef.Type}' is not supported by this factory.");
            }

            return entity;
        }

        private void ConstructEntity(
            EntityInstance entity,
            WorldEntityCreateContext context)
        {
            entity.AddComponent(definitionRuntimeFactory.CreateCollision(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateTransform(context.RoomSpatialID, context.LayerZ, context.Position));
        }

        private void ConstructProjectile(
            EntityInstance entity, 
            WorldEntityCreateContext context)
        {
            if (context is not ProjectileEntityCreateContext projectileContext)
                throw new InternalException(
                    ApplicationCode.EntityInstanceFactoryCode.InvalidProjectileContextType,
                    $"Expected ProjectileEntityCreateContext for Projectile entity, but got {context.GetType().Name}.");

            entity.AddComponent(definitionRuntimeFactory.CreateLifeTime(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateTriggeredEffect(entity.DefinitionID, projectileContext.SourceEntityID));
            entity.AddComponent(definitionRuntimeFactory.CreateAppearance(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateProjectile(entity.DefinitionID));

            // Set direction
            entity.GetComponent<ProjectileInstance>()!.Direction = projectileContext.Direction;
        }

        private void ConstructWorldObject(
            EntityInstance entity,
            WorldEntityCreateContext context)
        {
            entity.AddComponent(definitionRuntimeFactory.CreateAppearance(entity.DefinitionID));
        }

        private void ConstructCreature(
            EntityInstance entity,
            WorldEntityCreateContext context)
        {
            entity.AddComponent(definitionRuntimeFactory.CreateCharacteristic(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateInventory(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateAppearance(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateAction());
            entity.AddComponent(definitionRuntimeFactory.CreateEffectContainer());
            entity.AddComponent(definitionRuntimeFactory.CreateAI(entity.DefinitionID));

            // Initialize
            characteristicService.InitializeVitals(entity);
            characteristicService.InitializeCores(entity);
        }

        private void ConstructPlayer(
            EntityInstance entity,
            WorldEntityCreateContext context)
        {
            if (context is not PlayerEntityCreateContext playerContext)
                throw new InternalException(
                    ApplicationCode.EntityInstanceFactoryCode.InvalidPlayerContextType,
                    $"Expected PlayerEntityCreateContext for Player entity, but got {context.GetType().Name}.");

            entity.AddComponent(definitionRuntimeFactory.CreateCharacteristic(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateInventory(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateAppearance(entity.DefinitionID));
            entity.AddComponent(definitionRuntimeFactory.CreateAction());
            entity.AddComponent(definitionRuntimeFactory.CreateEffectContainer());
            entity.AddComponent(definitionRuntimeFactory.CreateEconomic());
            entity.AddComponent(definitionRuntimeFactory.CreateOwnership(playerContext.UserID, playerContext.PersonalRoomID));
            entity.AddComponent(definitionRuntimeFactory.CreateQuestProgress());

            // Initialize
            characteristicService.InitializeVitals(entity);
            characteristicService.InitializeCores(entity);
        }

        private void ConstructItem(
            EntityInstance entity,
            WorldEntityCreateContext context)
        {
            if (context is not WorldItemCreateContext itemContext)
                throw new InternalException(
                    ApplicationCode.EntityInstanceFactoryCode.InvalidItemContextType,
                    $"Expected InventoryEntityCreateContext for Item entity, but got {context.GetType().Name}.");

            entity.AddComponent(definitionRuntimeFactory.CreateWorldItemPayload(itemContext.Payload));
        }
        #endregion
    }
}