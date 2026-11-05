using Application.Service.EntityService;
using Application.Service.WorldService;
using Application.System.Abstraction;
using Application.System.Queue;
using Contract;
using Contract.Enum.MetaDomain.Effect;
using Domain.Runtime.EntityDomain.Component;
using Domain.Runtime.MetaDomain;

namespace Application.Service.MetaService
{
    public class EffectService : ITickService
    {
        #region Attributes
        private readonly WorldContext worldContext;
        private readonly CharacteristicService characteristicService;
        #endregion

        public EffectService(
            WorldContext worldContext,
            CharacteristicService characteristicService)
        {
            this.worldContext = worldContext;
            this.characteristicService = characteristicService;
        }

        #region Methods
        public void ApplyEffect(
            EffectContext effectContext)
        {
            var container = effectContext.Target.GetComponent<EffectContainerInstance>();
            if (container == null)
                return;

            var effect = effectContext.Effect;

            // Temporary
            if (effect.Duration > 0)
            {
                ApplyTemporary(container, effectContext);
                return;
            }

            // Instant: no duration on a vital (heal, damage, energy) is one-shot, never tracked
            if (AttributeDefinitions.Get(effect.AttributeType).DomainType == DomainType.Vital)
            {
                characteristicService.ApplyEffectLogic(effectContext);
                return;
            }

            // Permanent: no duration on a core (equipment) persists until removed
            ApplyPermanent(container, effectContext);
        }

        public void RemoveEffect(
            EffectContext effectContext)
        {
            var effectDef = effectContext.Effect;

            var container = effectContext.Target.GetComponent<EffectContainerInstance>();
            if (container == null)
                return;

            var existing = container.TrackingEffects.FirstOrDefault(e => e.DefinitionID == effectDef.Id);
            if (existing != null)
            {
                container.TrackingEffects.Remove(existing);
                characteristicService.ApplyEffectLogic(effectContext);
            }
        }

        public void Tick(
            float dt,
            CommandBuffer commandBuffer)
        {
            foreach (var entity in worldContext.GetEntities())
            {
                var container = entity.GetComponent<EffectContainerInstance>();
                if (container == null)
                    continue;

                // Iterate on a copy since effects may expire during iteration
                foreach (var effect in container.TrackingEffects.ToList())
                {
                    effect.TickDuration(dt);

                    if (effect.TickInterval(dt))
                    {
                        // Vital effects need to be ticking
                        characteristicService.ApplyEffectLogic(effect.Context);
                    }

                    if (effect.IsExpired())
                    {
                        container.TrackingEffects.Remove(effect);

                        // Core effects need recalculation after disappearing.
                        // Vital effects with duration simply stop ticking.
                        characteristicService.ApplyEffectLogic(effect.Context);
                    }
                }
            }
        }

        private void ApplyTemporary(
            EffectContainerInstance container,
            EffectContext context)
        {
            var definition = context.Effect;

            var existing = container.TrackingEffects.FirstOrDefault(e => e.DefinitionID == definition.Id);
            if (existing != null)
            {
                existing.ResetTimer(definition.Duration!.Value);
                return;
            }

            container.TrackingEffects.Add(new EffectInstance(
                definition.Id,
                context,
                definition.Duration,
                definition.Interval));

            characteristicService.ApplyEffectLogic(context);
        }

        private void ApplyPermanent(
            EffectContainerInstance container,
            EffectContext context)
        {
            var definition = context.Effect;

            var existing = container.TrackingEffects.FirstOrDefault(e => e.DefinitionID == definition.Id);
            if (existing != null)
                return;

            container.TrackingEffects.Add(new EffectInstance(
                definition.Id,
                context,
                null,
                definition.Interval));

            characteristicService.ApplyEffectLogic(context);
        }
        #endregion
    }
}