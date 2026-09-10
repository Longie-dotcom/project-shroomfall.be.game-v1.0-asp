using Application.Service.WorldService.Creation;
using AutoMapper;
using Contract.Common;
using Contract.DTO.Abstraction;
using Contract.DTO.Runtime.EntityDomain;
using Contract.DTO.Runtime.EntityDomain.Component;
using Contract.DTO.Runtime.MetaDomain;
using Contract.DTO.Runtime.WorldDomain;
using Domain.Abstraction;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using Domain.Runtime.WorldDomain.Spatial;

namespace Application.Mapper
{
    public class DTOMapper : Profile
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        public DTOMapper()
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
            CreateMap<ComponentInstance, ComponentInstanceDTO>()
                .Include<ActionInstance, ActionInstanceDTO>()
                .Include<AIInstance, AIInstanceDTO>()
                .Include<AppearanceInstance, AppearanceInstanceDTO>()
                .Include<CollisionInstance, CollisionInstanceDTO>()
                .Include<CharacteristicInstance, CharacteristicInstanceDTO>()
                .Include<EffectContainerInstance, EffectContainerInstanceDTO>()
                .Include<InventoryInstance, InventoryInstanceDTO>()
                .Include<LifetimeInstance, LifetimeInstanceDTO>()
                .Include<OwnershipInstance, OwnershipInstanceDTO>()
                .Include<ProjectileInstance, ProjectileInstanceDTO>()
                .Include<TransformInstance, TransformInstanceDTO>()
                .Include<TriggeredEffectInstance, TriggeredEffectInstanceDTO>()
                .Include<WorldItemPayloadInstance, WorldItemPayloadInstanceDTO>();

            // Action Instance
            CreateMap<ActionInstance, ActionInstanceDTO>();

            // AI Instance
            CreateMap<AIInstance, AIInstanceDTO>();

            // Appearance Instance
            CreateMap<AppearanceInstance, AppearanceInstanceDTO>();

            // Collision Instance
            CreateMap<CollisionInstance, CollisionInstanceDTO>();

            // Characteristic Instance
            CreateMap<CharacteristicInstance, CharacteristicInstanceDTO>()
                .ForMember(dest => dest.Cores, opt => opt.MapFrom(src => src.GetCores().Select(kvp => new AttributeValueInstanceDTO { AttributeType = kvp.Key, Value = kvp.Value })))
                .ForMember(dest => dest.Vitals, opt => opt.MapFrom(src => src.GetVitals().Select(kvp => new AttributeValueInstanceDTO { AttributeType = kvp.Key, Value = kvp.Value })));

            // Effect Container Instance
            CreateMap<EffectContainerInstance, EffectContainerInstanceDTO>();

            // Inventory Instance
            CreateMap<InventoryInstance, InventoryInstanceDTO>();

            // Lifetime Instance
            CreateMap<LifetimeInstance, LifetimeInstanceDTO>();

            // Ownership Instance
            CreateMap<OwnershipInstance, OwnershipInstanceDTO>();

            // Projectile Instance
            CreateMap<ProjectileInstance, ProjectileInstanceDTO>();

            // Transform Instance
            CreateMap<TransformInstance, TransformInstanceDTO>();

            // Triggered Effect Instance
            CreateMap<TriggeredEffectInstance, TriggeredEffectInstanceDTO>();

            // World Item Payload Instance
            CreateMap<WorldItemPayloadInstance, WorldItemPayloadInstanceDTO>();

            // Entity
            CreateMap<EntityInstance, EntityInstanceDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID));

            // ─────────────────────────────
            // Meta Domain
            // ─────────────────────────────
            // Effect
            CreateMap<EffectInstance, EffectInstanceDTO>();

            // Item
            CreateMap<ItemInstance, ItemInstanceDTO>();

            // ─────────────────────────────
            // World Domain
            // ─────────────────────────────
            // Room
            CreateMap<RoomSpatial, RoomSpatialDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID));
            CreateMap<RoomInstance, RoomInstanceDTO>();
        }
        #endregion
    }
}