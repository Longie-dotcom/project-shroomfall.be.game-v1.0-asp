using Application.Interface.Cache;
using Application.Interface.Realtime.Events;
using Application.Interface.Realtime.Events.Game;
using Application.Service.EntityService;
using Contract;
using Contract.DTO.Definition.EntityDomain.Component;
using Contract.DTO.Definition.MetaDomain;
using Contract.Enum.EntityDomain;
using Contract.Enum.MetaDomain.Effect;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;

namespace Application.Service.MetaService
{
    public class VitalService
    {
        #region Attributes
        private readonly ICacheProvider cacheProvider;
        private readonly IEventBus eventBus;
        #endregion

        #region Properties
        #endregion

        public VitalService(
            ICacheProvider cacheProvider,
            IEventBus eventBus)
        {
            this.cacheProvider = cacheProvider;
            this.eventBus = eventBus;
        }

        #region Offensive / Restorative Health
        public (VitalChangedRecord? target, VitalChangedRecord? source) ApplyOffensiveHealth(
            EffectContext effectContext)
        {
            var target = effectContext.Target;
            var source = effectContext.Source;
            var effect = effectContext.Effect;

            var targetCharacteristic = target.GetComponent<CharacteristicInstance>();
            if (targetCharacteristic == null) return (null, null);

            var sourceCharacteristic = source?.GetComponent<CharacteristicInstance>();

            var healthAttribute = GetScaledAttribute(targetCharacteristic, AttributeType.Health);
            if (healthAttribute == null) return (null, null);

            var (config, _, maxHealth) = healthAttribute.Value;
            VitalChangeReason reason = VitalChangeReason.Damage;

            //--------------------------------------------------------
            // Source offensive power
            //--------------------------------------------------------
            var powerType = GetPowerType(effect.AttributeType);
            float offensivePower = sourceCharacteristic != null ? sourceCharacteristic.GetCore(powerType) : 0f;

            //--------------------------------------------------------
            // Scale by Effect
            //--------------------------------------------------------
            float rawDamage = ResolveRawDelta(offensivePower, effect);

            //--------------------------------------------------------
            // Resistance / Penetration
            //--------------------------------------------------------
            var resistanceType = GetResistanceType(effect.AttributeType);
            var penetrationType = GetPenetrationType(effect.AttributeType);

            float resistance = targetCharacteristic.GetCore(resistanceType);
            float penetration = sourceCharacteristic != null ? sourceCharacteristic.GetCore(penetrationType) : 0f;

            float effectiveResistance = Math.Max(0f, resistance - penetration);
            float mitigation = Math.Clamp(1f - effectiveResistance, 0f, 2f);
            float finalDamage = rawDamage * mitigation;

            //--------------------------------------------------------
            // Critical Chance
            //--------------------------------------------------------
            if (sourceCharacteristic != null && finalDamage > 0f)
            {
                float criticalChance = sourceCharacteristic.GetCore(AttributeType.CriticalChance);
                if (criticalChance > Random.Shared.NextSingle())
                {
                    finalDamage *= Constraint.CRITICAL_DAMAGE_VALUE;
                    reason = VitalChangeReason.Critical;
                }
            }

            //--------------------------------------------------------
            // Block
            //--------------------------------------------------------
            // Note: As written, a successful block will completely negate a critical hit.
            if (finalDamage > 0 && targetCharacteristic.GetCore(AttributeType.BlockChance) > Random.Shared.NextSingle())
            {
                reason = VitalChangeReason.Block;
                finalDamage = 0f;
            }

            //--------------------------------------------------------
            // Apply Damage
            //--------------------------------------------------------
            float previous = targetCharacteristic.GetVital(AttributeType.Health);
            float current = Math.Clamp(previous - finalDamage, config.Min, maxHealth);
            targetCharacteristic.SetVital(AttributeType.Health, current);

            var transform = target.GetComponent<TransformInstance>();
            if (transform != null)
            {
                eventBus.Publish(new EntityActedEvent(
                    target.ID,
                    transform.RoomSpatialID,
                    transform.Position,
                    transform.FacingDirection,
                    EntityAction.DAMAGED,
                    null
                ));
            }

            //--------------------------------------------------------
            // Life Steal
            //--------------------------------------------------------
            VitalChangedRecord? sourceChanged = null;
            if (sourceCharacteristic != null && finalDamage > 0f && source != null)
            {
                float lifeSteal = sourceCharacteristic.GetCore(AttributeType.LifeSteal);
                if (lifeSteal > 0f)
                {
                    var sourceHealth = GetScaledAttribute(sourceCharacteristic, AttributeType.Health);
                    if (sourceHealth != null)
                    {
                        var (sourceConfig, _, sourceMaxHealth) = sourceHealth.Value;
                        float sourcePrevious = sourceCharacteristic.GetVital(AttributeType.Health);
                        float sourceCurrent = Math.Clamp(sourcePrevious + finalDamage * lifeSteal, sourceConfig.Min, sourceMaxHealth);

                        sourceCharacteristic.SetVital(AttributeType.Health, sourceCurrent);

                        sourceChanged = new VitalChangedRecord
                        {
                            EntityInstanceID = source.ID,
                            SourceInstanceID = target.ID,
                            Vital = AttributeType.Health,
                            PreviousValue = sourcePrevious,
                            CurrentValue = sourceCurrent,
                            Reason = VitalChangeReason.LifeSteal
                        };
                    }
                }
            }

            return (
                new VitalChangedRecord
                {
                    EntityInstanceID = target.ID,
                    SourceInstanceID = source?.ID ?? string.Empty,
                    Vital = AttributeType.Health,
                    PreviousValue = previous,
                    CurrentValue = current,
                    Reason = reason
                },
                sourceChanged);
        }

        public (VitalChangedRecord? target, VitalChangedRecord? source) ApplyRestorativeHealth(
            EffectContext effectContext)
        {
            var target = effectContext.Target;
            var source = effectContext.Source;
            var effect = effectContext.Effect;

            var characteristic = target.GetComponent<CharacteristicInstance>();
            if (characteristic == null) return (null, null);

            var healthAttribute = GetScaledAttribute(characteristic, AttributeType.Health);
            if (healthAttribute == null) return (null, null);

            var (config, _, maxHealth) = healthAttribute.Value;

            float healAmount = ResolveRawDelta(0f, effect);
            float previous = characteristic.GetVital(AttributeType.Health);
            float current = Math.Clamp(previous + healAmount, config.Min, maxHealth);

            characteristic.SetVital(AttributeType.Health, current);

            return (
                new VitalChangedRecord
                {
                    EntityInstanceID = target.ID,
                    SourceInstanceID = source?.ID ?? string.Empty,
                    Vital = AttributeType.Health,
                    PreviousValue = previous,
                    CurrentValue = current,
                    Reason = VitalChangeReason.Heal
                },
                null);
        }
        #endregion

        #region Offensive / Restorative Energy
        public (VitalChangedRecord? target, VitalChangedRecord? source) ApplyOffensiveEnergy(
            EffectContext effectContext)
        {
            var target = effectContext.Target;
            var source = effectContext.Source;
            var effect = effectContext.Effect;

            var characteristic = target.GetComponent<CharacteristicInstance>();
            if (characteristic == null) return (null, null);

            var energyAttribute = GetScaledAttribute(characteristic, AttributeType.Energy);
            if (energyAttribute == null) return (null, null);

            var (config, _, maxEnergy) = energyAttribute.Value;

            float consumeAmount = ResolveRawDelta(0f, effect);
            float previous = characteristic.GetVital(AttributeType.Energy);
            float current = Math.Clamp(previous - consumeAmount, config.Min, maxEnergy);

            characteristic.SetVital(AttributeType.Energy, current);

            return (
                new VitalChangedRecord
                {
                    EntityInstanceID = target.ID,
                    SourceInstanceID = source?.ID ?? string.Empty,
                    Vital = AttributeType.Energy,
                    PreviousValue = previous,
                    CurrentValue = current,
                    Reason = VitalChangeReason.EnergyConsume
                },
                null);
        }

        public (VitalChangedRecord? target, VitalChangedRecord? source) ApplyRestorativeEnergy(
            EffectContext effectContext)
        {
            var target = effectContext.Target;
            var source = effectContext.Source;
            var effect = effectContext.Effect;

            var characteristic = target.GetComponent<CharacteristicInstance>();
            if (characteristic == null) return (null, null);

            var energyAttribute = GetScaledAttribute(characteristic, AttributeType.Energy);
            if (energyAttribute == null) return (null, null);

            var (config, _, maxEnergy) = energyAttribute.Value;

            float restoreAmount = ResolveRawDelta(0f, effect);
            float previous = characteristic.GetVital(AttributeType.Energy);
            float current = Math.Clamp(previous + restoreAmount, config.Min, maxEnergy);

            characteristic.SetVital(AttributeType.Energy, current);

            return (
                new VitalChangedRecord
                {
                    EntityInstanceID = target.ID,
                    SourceInstanceID = source?.ID ?? string.Empty,
                    Vital = AttributeType.Energy,
                    PreviousValue = previous,
                    CurrentValue = current,
                    Reason = VitalChangeReason.EnergyRestore
                },
                null);
        }
        #endregion

        #region Helpers
        private float ResolveRawDelta(float baseValue, EffectDefinitionDTO effect)
        {
            return effect.Type switch
            {
                EffectType.Flat => baseValue + effect.Value,
                EffectType.Percentage => baseValue * (1 + effect.Value),
                _ => baseValue
            };
        }

        private static AttributeType GetPowerType(AttributeType damageType) => damageType switch
        {
            AttributeType.PhysicalDamage => AttributeType.PhysicalPower,
            AttributeType.FireDamage => AttributeType.FirePower,
            AttributeType.IceDamage => AttributeType.IcePower,
            AttributeType.EarthDamage => AttributeType.EarthPower,
            AttributeType.DarkDamage => AttributeType.DarkPower,
            AttributeType.LightDamage => AttributeType.LightPower,
            _ => AttributeType.PhysicalPower,
        };

        private static AttributeType GetResistanceType(AttributeType damageType) => damageType switch
        {
            AttributeType.PhysicalDamage => AttributeType.PhysicalResistance,
            AttributeType.FireDamage => AttributeType.FireResistance,
            AttributeType.IceDamage => AttributeType.IceResistance,
            AttributeType.EarthDamage => AttributeType.EarthResistance,
            AttributeType.DarkDamage => AttributeType.DarkResistance,
            AttributeType.LightDamage => AttributeType.LightResistance,
            _ => AttributeType.PhysicalResistance
        };

        private static AttributeType GetPenetrationType(AttributeType damageType) => damageType switch
        {
            AttributeType.PhysicalDamage => AttributeType.PhysicalPenetration,
            AttributeType.FireDamage => AttributeType.FirePenetration,
            AttributeType.IceDamage => AttributeType.IcePenetration,
            AttributeType.EarthDamage => AttributeType.EarthPenetration,
            AttributeType.DarkDamage => AttributeType.DarkPenetration,
            AttributeType.LightDamage => AttributeType.LightPenetration,
            _ => AttributeType.PhysicalPenetration
        };

        private (AttributeValueDTO Config, float BaseValue, float MaxValue)? GetScaledAttribute(
            CharacteristicInstance characteristic,
            AttributeType type)
        {
            var pair = cacheProvider.Characteristic.GetAttributeValue(
                characteristic.DefinitionID,
                characteristic.CurrentLevel,
                type);

            if (pair == null) return null;

            var (attribute, growth) = pair.Value;

            return (
                attribute,
                attribute.BaseValue + growth.GrowthValue,
                attribute.Max + growth.GrowthValue
            );
        }
        #endregion
    }
}