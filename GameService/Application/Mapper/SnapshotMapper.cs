using AutoMapper;
using Contract.Common;
using Contract.Enum.MetaDomain.Effect;
using Domain.Abstraction;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using Domain.Runtime.WorldDomain.Spatial;
using Domain.Snapshot.EntityDomain;
using Domain.Snapshot.EntityDomain.Component;
using Domain.Snapshot.MetaDomain;
using Domain.Snapshot.WorldDomain;

namespace Application.Mapper
{
    public class SnapshotMapper : Profile
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        public SnapshotMapper()
        {
            FromCommon();
            FromRuntime();
        }

        #region Methods
        public void FromCommon()
        {
            // Common
            CreateMap<HSV, HSV>();
            CreateMap<Vector2, Vector2>();
        }

        public void FromRuntime()
        {
            // ─────────────────────────────
            // Entity Domain
            // ─────────────────────────────
            CreateMap<ComponentInstance, ComponentSnapshot>()
                .ForMember(dest => dest.DefinitionID, opt => opt.MapFrom(src => src.DefinitionID.ToString()))
                .ForMember(dest => dest.EntityDefinitionID, opt => opt.MapFrom(src => src.Entity.DefinitionID.ToString()))
                .Include<ActionInstance, ActionSnapshot>()
                .Include<AIInstance, AISnapshot>()
                .Include<AppearanceInstance, AppearanceSnapshot>()
                .Include<CollisionInstance, CollisionSnapshot>()
                .Include<CharacteristicInstance, CharacteristicSnapshot>()
                .Include<EconomicInstance, EconomicSnapshot>()
                .Include<EffectContainerInstance, EffectContainerSnapshot>()
                .Include<InventoryInstance, InventorySnapshot>()
                .Include<LifetimeInstance, LifetimeSnapshot>()
                .Include<OwnershipInstance, OwnershipSnapshot>()
                .Include<ProjectileInstance, ProjectileSnapshot>()
                .Include<QuestProgressInstance, QuestProgressSnapshot>()
                .Include<TransformInstance, TransformSnapshot>()
                .Include<TriggeredEffectInstance, TriggeredEffectSnapshot>()
                .Include<WorldItemPayloadInstance, WorldItemPayloadSnapshot>();

            // Action Snapshot
            CreateMap<ActionInstance, ActionSnapshot>();

            // AI Snapshot
            CreateMap<AIInstance, AISnapshot>();

            // Appearance Snapshot
            CreateMap<AppearanceInstance, AppearanceSnapshot>();

            // Collision Snapshot
            CreateMap<CollisionInstance, CollisionSnapshot>();

            // Characteristic Snapshot
            CreateMap<CharacteristicInstance, CharacteristicSnapshot>()
                .ForMember(dest => dest.Vitals, opt => opt.MapFrom(src => new Dictionary<AttributeType, float>(src.GetVitals())));

            // Lifetime Snapshot
            CreateMap<EconomicInstance, EconomicSnapshot>();

            // Effect Container Snapshot
            CreateMap<EffectContainerInstance, EffectContainerSnapshot>();

            // Inventory Snapshot
            CreateMap<InventoryInstance, InventorySnapshot>();

            // Lifetime Snapshot
            CreateMap<LifetimeInstance, LifetimeSnapshot>();

            // Ownership Snapshot
            CreateMap<OwnershipInstance, OwnershipSnapshot>();

            // Projectile Snapshot
            CreateMap<ProjectileInstance, ProjectileSnapshot>();

            // Transform Snapshot
            CreateMap<QuestProgressInstance, QuestProgressSnapshot>();
            CreateMap<QuestProgressEntry, QuestProgressEntrySnapshot>();
            CreateMap<IdentificationProgress, IdentificationProgressSnapshot>();

            // Transform Snapshot
            CreateMap<TransformInstance, TransformSnapshot>();

            // Triggered Effect Snapshot
            CreateMap<TriggeredEffectInstance, TriggeredEffectSnapshot>();

            // World Item Payload Snapshot
            CreateMap<WorldItemPayloadInstance, WorldItemPayloadSnapshot>();

            // Entity
            CreateMap<EntityInstance, EntitySnapshot>()
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components));

            // ─────────────────────────────
            // Meta Domain
            // ─────────────────────────────
            // Effect
            CreateMap<EffectInstance, EffectSnapshot>();
            
            // Item
            CreateMap<ItemInstance, ItemSnapshot>();

            // ─────────────────────────────
            // World Domain
            // ─────────────────────────────
            // Room
            CreateMap<RoomSpatial, RoomSnapshot>();
        }
        #endregion
    }
}