using Application.Interface.Cache;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Service.MetaService;
using Application.System.Abstraction;
using Application.System.Queue;
using Contract;
using Contract.DTO.Definition.EntityDomain.Component;
using Contract.DTO.Definition.MetaDomain;
using Contract.Enum.MetaDomain.Effect;
using Domain.Runtime.EntityDomain;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;
using System.Collections.Concurrent;

namespace Application.Service.EntityService
{
    public sealed class VitalChangedRecord
    {
        public required string EntityInstanceID { get; init; }
        public required string SourceInstanceID { get; init; }
        public required AttributeType Vital { get; init; }
        public required float PreviousValue { get; init; }
        public required float CurrentValue { get; init; }
        public required VitalChangeReason Reason { get; init; }
    }

    public class CharacteristicService : ITickService
    {
        #region Attributes
        private readonly IEventBus eventBus;
        private readonly ICacheProvider cacheProvider;
        private readonly ConcurrentQueue<VitalChangedRecord> pendingVitalChanges = new();
        private readonly VitalService vitalService;
        #endregion

        #region Properties
        #endregion

        public CharacteristicService(
            IEventBus eventBus,
            ICacheProvider cacheProvider,
            VitalService vitalService)
        {
            this.eventBus = eventBus;
            this.cacheProvider = cacheProvider;
            this.vitalService = vitalService;
        }

        #region Methods
        public void Tick(
            float dt, 
            CommandBuffer buffer)
        {
            while (pendingVitalChanges.TryDequeue(out var change))
            {
                buffer.Commands.Enqueue(
                    new VitalThresholdCommand(
                        change.EntityInstanceID,
                        change.SourceInstanceID,
                        change.Vital,
                        change.PreviousValue,
                        change.CurrentValue));
            }
        }

        public void InitializeVitals(
            EntityInstance entity)
        {
            var characteristic = entity.GetComponent<CharacteristicInstance>();

            if (characteristic == null) return;
            
            foreach (var attrDef in AttributeDefinitions.AllList())
            {
                if (attrDef.DomainType != DomainType.Vital)
                    continue;

                var scaledAttr = GetScaledAttribute(characteristic, attrDef.Type);
                if (scaledAttr == null) continue;

                var (config, maxCapacity) = scaledAttr.Value;
                maxCapacity = Math.Clamp(maxCapacity, config.Min, config.Max);
                
                characteristic.SetVital(attrDef.Type, maxCapacity);
            }
        }

        public void InitializeCores(
            EntityInstance entity)
        {
            var characteristic = entity.GetComponent<CharacteristicInstance>();
            var effectContainer = entity.GetComponent<EffectContainerInstance>();

            if (characteristic == null || effectContainer == null)
                return;

            // Execute raw logic with no side effects
            CalculateCoresInternal(characteristic, effectContainer);
        }

        public void ApplyEffectLogic(
            EffectContext context)
        {
            var effectDef = context.Effect;
            var attribute = AttributeDefinitions.Get(effectDef.AttributeType);

            switch (attribute.DomainType)
            {
                case DomainType.Vital:
                    ApplyVital(context);
                    break;

                case DomainType.Core:
                    ApplyCore(context.Target);
                    break;
            }
        }

        private void ApplyVital(
            EffectContext effectContext)
        {
            // Extract effect context
            var target = effectContext.Target;
            var effectDef = effectContext.Effect;
            var source = effectContext.Source;

            (VitalChangedRecord? targetRecord, VitalChangedRecord? sourceRecord) result = (null, null);

            // Dispatch to the proper vital resolver based on the semantic Category
            var attribute = AttributeDefinitions.Get(effectDef.AttributeType);

            switch (attribute.Category)
            {
                case AttributeCategory.OffensiveHealth:
                    result = vitalService.ApplyOffensiveHealth(effectContext);
                    break;

                case AttributeCategory.RestorativeHealth:
                    result = vitalService.ApplyRestorativeHealth(effectContext);
                    break;

                case AttributeCategory.OffensiveEnergy:
                    result = vitalService.ApplyOffensiveEnergy(effectContext);
                    break;

                case AttributeCategory.RestorativeEnergy:
                    result = vitalService.ApplyRestorativeEnergy(effectContext);
                    break;
            }

            // Queue valid records into the Command buffer cycle
            if (result.targetRecord != null)
            {
                // Validate transform for publishing
                var targetTransform = target.GetComponent<TransformInstance>();
                if (targetTransform == null)
                    return;

                PublishVitalChange(result.targetRecord, targetTransform);
            }

            if (result.sourceRecord != null && source != null)
            {
                // Validate transform for publishing
                var sourceTransform = source.GetComponent<TransformInstance>();
                if (sourceTransform == null)
                    return;

                PublishVitalChange(result.sourceRecord, sourceTransform);
            }
        }

        private void ApplyCore(
            EntityInstance entity)
        {
            var characteristic = entity.GetComponent<CharacteristicInstance>();
            var effectContainer = entity.GetComponent<EffectContainerInstance>();

            if (characteristic == null || effectContainer == null) return;

            // Run the core mathematical calculations
            CalculateCoresInternal(characteristic, effectContainer);

            // Perform the infrastructure side-effects (Syncing / Event Bus)
            eventBus.Publish(new PlayerCharacteristicSyncEvent(
                entityInstanceId: entity.ID,
                characteristicInstance: characteristic
            ));
        }

        private void CalculateCoresInternal(
            CharacteristicInstance characteristic,
            EffectContainerInstance effectContainer)
        {
            var activeEffectsByAttribute = new Dictionary<AttributeType, List<EffectDefinitionDTO>>();
            foreach (var activeEffect in effectContainer.TrackingEffects)
            {
                var effectDef = activeEffect.Context.Effect;
                if (!activeEffectsByAttribute.TryGetValue(effectDef.AttributeType, out var list))
                {
                    list = new List<EffectDefinitionDTO>();
                    activeEffectsByAttribute[effectDef.AttributeType] = list;
                }
                list.Add(effectDef);
            }

            var allAttrs = AttributeDefinitions.AllList();
            if (allAttrs == null) 
                return;

            foreach (var attrDef in allAttrs)
            {
                // 1. Check if it's a Core
                if (attrDef.DomainType != DomainType.Core)
                    continue;

                // 2. Check if the attribute exists on this entity
                var scaledAttr = GetScaledAttribute(characteristic, attrDef.Type);
                if (scaledAttr == null)
                    continue;

                float flat = 0f;
                float percent = 0f;
                float multiplier = 1f;

                if (activeEffectsByAttribute.TryGetValue(attrDef.Type, out var matchingEffects))
                {
                    foreach (var effect in matchingEffects)
                    {
                        switch (effect?.Type)
                        {
                            case EffectType.Flat: flat += effect.Value; break;
                            case EffectType.Percentage: percent += effect.Value; break;
                        }
                    }
                }

                var (config, scaledBaseValue) = scaledAttr.Value;
                float result = (scaledBaseValue + flat) * (1f + percent) * multiplier;
                result = Math.Clamp(result, config.Min, config.Max);
                characteristic.SetCore(attrDef.Type, result);
            }
        }

        public (AttributeValueDTO Config, float ScaledValue)? GetScaledAttribute(
            CharacteristicInstance characteristic,
            AttributeType type)
        {
            var attributePair = cacheProvider.Characteristic.GetAttributeValue(
                characteristic.DefinitionID,
                characteristic.CurrentLevel,
                type);

            if (attributePair == null) return null;

            var (attribute, growth) = attributePair.Value;
            return (attribute, attribute.BaseValue + growth.GrowthValue);
        }

        private void PublishVitalChange(
            VitalChangedRecord? record,
            TransformInstance transform)
        {
            if (record == null)
                return;

            pendingVitalChanges.Enqueue(record);

            eventBus.Publish(new EntityVitalChangedEvent(
                entityInstanceId: record.EntityInstanceID,
                roomSpatialId: transform.RoomSpatialID,
                attributeType: record.Vital,
                newValue: record.CurrentValue,
                vitalChangeReason: record.Reason));
        }
        #endregion
    }
}